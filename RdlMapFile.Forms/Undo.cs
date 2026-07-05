

using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using RdlMapFile.Resources;

namespace Majorsilence.Reporting.RdlMapFile
{
	internal class Undo
	{ 
		Stack _actions;
		bool _Undoing = false;
		XmlDocument _doc;
		UndoGroup _currentUndoGroup = null;
		object _pState=null;					// state saved with previous changing event
		int _UndoLevels;
		bool _GroupsOnly=false;

		internal Undo(XmlDocument doc, int levels)
		{ 
			_doc = doc;
			_UndoLevels = levels;				// we don't currently support; need to write special Stack
			_doc.NodeChanging +=new XmlNodeChangedEventHandler(NodeChanging);
			_doc.NodeChanged += new XmlNodeChangedEventHandler(NodeChanged);
			_doc.NodeInserting +=new XmlNodeChangedEventHandler(NodeChanging);
			_doc.NodeInserted += new XmlNodeChangedEventHandler(NodeChanged);
			_doc.NodeRemoving +=new XmlNodeChangedEventHandler(NodeChanging);
			_doc.NodeRemoved += new XmlNodeChangedEventHandler(NodeChanged);
			_actions = new Stack();
		}

		internal bool GroupsOnly
		{
			get {return _GroupsOnly;}
			set {_GroupsOnly = value;}
		}

		internal bool CanUndo
		{
			get {return _actions.Count > 0;}
		}

		internal string Description
		{
			get
			{
				if (_actions.Count == 0)
					return "";

				UndoItem ui = (UndoItem) _actions.Peek();
				return ui.GetDescription();
			}
		}

		internal void Reset()
		{
			_actions.Clear();
		}

		internal bool Undoing
		{
			get {return _Undoing;}
			set {_Undoing = value;}
		}

		private void NodeChanging(object sender, XmlNodeChangedEventArgs e)
		{
			if (_Undoing)
				return;

			switch (e.Action)
			{
				case XmlNodeChangedAction.Insert:
					_pState = NodeInsertedUndo.PreviousState(e);
					break;
				case XmlNodeChangedAction.Remove:
					_pState = NodeRemovedUndo.PreviousState(e);
					break;
				case XmlNodeChangedAction.Change:
					_pState = NodeChangedUndo.PreviousState(e);
					break;
				default:
					throw new Exception(Strings.Undo_Error_UnknownAction);
			}
		}

		private void NodeChanged(object sender, XmlNodeChangedEventArgs e)
		{
			if (_Undoing)
			{
				// if we're undoing ignore the event since it is the result of an undo
				_pState = null;
				_Undoing = false;
				return;
			}

			UndoItem undo = null;
			switch (e.Action)
			{
				case XmlNodeChangedAction.Insert:
					undo = new NodeInsertedUndo(e, _pState);
					break;
				case XmlNodeChangedAction.Remove:
					undo = new NodeRemovedUndo(e, _pState);
					break;
				case XmlNodeChangedAction.Change:
					undo = new NodeChangedUndo(e, _pState);
					break;
				default:
					throw new Exception(Strings.Undo_Error_UnknownAction);
			}
			_pState = null;
			if (_currentUndoGroup != null)
			{
				_currentUndoGroup.AddUndoItem(undo);
			}
			else if (GroupsOnly)
			{
				_pState = null;
			}
			else
			{
				_actions.Push(undo);
			}
		}

		internal void undo()
		{
			UndoItem undoItem = null;

			if (_actions.Count == 0)
				return;

			undoItem = (UndoItem)_actions.Pop();

			_Undoing = true;
			undoItem.Undo();
		}

		internal void StartUndoGroup(String description)
		{
			UndoGroup ug = new UndoGroup(this, description);
			_currentUndoGroup = ug;
			_actions.Push(ug);
		}
		
		internal void EndUndoGroup()
		{
			EndUndoGroup(true);			// we want to keep the undo items on the stack
		}	

		internal void EndUndoGroup(bool keepChanges)
		{
			if (_currentUndoGroup == null)
				return;

			// group contains no items; or user doesn't want changes to part of undo
			//   
			if (_currentUndoGroup.Count == 0 || !keepChanges)	
			{
				UndoGroup ug = _actions.Pop() as UndoGroup;	// get rid of the empty group
				if (ug != _currentUndoGroup)		
					throw new Exception(Strings.Undo_Error_EndGroupNotMatchStartGroup);
			}
			_currentUndoGroup = null;
		}
	}

    internal interface UndoItem
    {
        void Undo();
        String GetDescription();
    }

    internal class NodeInsertedUndo : UndoItem
    {
        XmlNode iNode;

        public NodeInsertedUndo(XmlNodeChangedEventArgs e, object previous)
        {
            iNode = e.Node;
        }

        public void Undo()
        {
            XmlNode parent = iNode.ParentNode;
			if (parent == null)		// happens with attributes but doesn't affect the undo??
			{
				return;
			}
            parent.RemoveChild(iNode);
        }

        public String GetDescription()
        {
			return Strings.NodeInsertedUndo_Undo_insert;	// could be more explicit using XmlNodeType but ...
        }
	
		static internal object PreviousState(XmlNodeChangedEventArgs e)
		{
			return null;
		}
	}

    internal class NodeRemovedUndo : UndoItem
    {
        internal XmlNode removedNode;
        internal XmlNode parentNode;
        internal XmlNode nextSibling;

        internal NodeRemovedUndo(XmlNodeChangedEventArgs e, object previous)
        {
            removedNode = e.Node;
            parentNode = e.OldParent;
            nextSibling = previous as XmlNode;
        }

        public void Undo()
        {
            parentNode.InsertBefore(removedNode, nextSibling);
        }

        public String GetDescription()
        {
			return Strings.NodeRemovedUndo_Undo_remove;		// could be more specific using NodeType
        }

		static internal object PreviousState(XmlNodeChangedEventArgs e)
		{
			return e.Node.NextSibling;
		}
	}


    /**
     * Can be used for both CharacterData and ProcessingInstruction nodes.
     */
    internal class NodeChangedUndo : UndoItem
    {
        String oldValue;
        XmlNode node;

        internal NodeChangedUndo(XmlNodeChangedEventArgs e, object pValue)
        {
			oldValue = pValue as string;
            node = e.Node;		
        }

        public void Undo()
        {
			node.Value = oldValue;
        }

        public String GetDescription()
        {
			return Strings.NodeChangedUndo_Undo_change;
        }
	
		static internal object PreviousState(XmlNodeChangedEventArgs e)
		{
			return e.Node.Value;
		}
	}

    /**
     * Groups several undo events into one transaction.  Needed when one
	 * user action corresponds to multiple dom events
     * */
    internal class UndoGroup : UndoItem
    {
		Undo _undo;
        string description;
        List<UndoItem> undoItems = new List<UndoItem>();

        internal UndoGroup(Undo undo, String description)
        {
			_undo = undo;
            this.description = description;
        }

        internal void AddUndoItem(UndoItem ui)
        {
            undoItems.Add(ui);
        }

		internal int Count
		{
			get {return undoItems.Count;}
		}

        public void Undo()
        {
			// loop thru group items backwards
			for (int i=undoItems.Count-1; i >= 0; i--)
            {
				UndoItem ui = undoItems[i] as UndoItem;
                _undo.Undoing = true;
                ui.Undo();
            }
        }

        public String GetDescription()
        {
            return description;
        }
    }

}