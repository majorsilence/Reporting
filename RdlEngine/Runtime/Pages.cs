

using Majorsilence.Reporting.RdlEngine.Resources;
using System;
using System.Collections;
using System.Collections.Generic;
using Majorsilence.Forms.Drawing;
using System.Drawing;  // value types (Color, Point, Size, Rectangle, ...) come from System.Drawing.Primitives

namespace Majorsilence.Reporting.Rdl
{
    ///<summary>
    /// Represents all the pages of a report.  Needed when you need
    /// render based on pages.  e.g. PDF
    ///</summary>
    public class Pages : IEnumerable, IDisposable
    {
        Bitmap _bm;                     // bitmap to build graphics object 
        Graphics _g;                    // graphics object
        Report _report;					// owner report
        List<Page> _pages;              // array of pages
        Page _currentPage;              // the current page; 1st page if null
        float _BottomOfPage;            // the bottom of the page
        float _PageHeight;              // default height for all pages
        float _PageWidth;               // default width for all pages

        public Pages(Report r)
        {
            _report = r;
            _pages = new List<Page>();  // array of Page objects

            _bm = new Bitmap(10, 10);   // create a small bitmap to base our graphics
            _g = Graphics.FromImage(_bm);
        }

        internal Report Report
        {
            get { return _report; }
        }

        public Page this[int index]
        {
            get { return _pages[index]; }
        }

        public int Count
        {
            get { return _pages.Count; }
        }

        public void AddPage(Page p)
        {
            _pages.Add(p);
            _currentPage = p;
        }

        public void NextOrNew()
        {
            if (_currentPage == this.LastPage)
                AddPage(new Page(PageCount + 1));
            else
            {
                _currentPage = _pages[_currentPage.PageNumber];
                _currentPage.SetEmpty();
            }
            //Allows using PageNumber in report body.
            //Important! This feature is NOT included in RDL specification!
            //PageNumber will be wrong if element using it will cause carry to next page after render.
            Report.PageNumber = _currentPage.PageNumber;
        }

        /// <summary>
        /// CleanUp should be called after every render to reduce resource utilization.
        /// </summary>
        public void CleanUp()
        {
            if (_g != null)
            {
                _g.Dispose();
                _g = null;
            }
            if (_bm != null)
            {
                _bm.Dispose();
                _bm = null;
            }
        }

        public void SortPageItems()
        {
            foreach (Page p in this)
            {
                p.SortPageItems();
            }
        }

        public float BottomOfPage
        {
            get { return _BottomOfPage; }
            set { _BottomOfPage = value; }
        }

        public Page CurrentPage
        {
            get
            {
                if (_currentPage != null)
                    return _currentPage;

                if (_pages.Count >= 1)
                {
                    _currentPage = _pages[0];
                    return _currentPage;
                }

                return null;
            }

            set
            {
                _currentPage = value;
#if DEBUG
                if (value == null)
                    return;
                foreach (Page p in _pages)
                {
                    if (p == value)
                        return;
                }
                throw new Exception(Strings.Pages_Error_CurrentPageMustInList);
#endif
            }
        }

        public Page FirstPage
        {
            get
            {
                if (_pages.Count <= 0)
                    return null;
                else
                    return _pages[0];
            }
        }

        public Page LastPage
        {
            get
            {
                if (_pages.Count <= 0)
                    return null;
                else
                    return _pages[_pages.Count - 1];
            }
        }

        public float PageHeight
        {
            get { return _PageHeight; }
            set { _PageHeight = value; }
        }

        public float PageWidth
        {
            get { return _PageWidth; }
            set { _PageWidth = value; }
        }

        public void RemoveLastPage()
        {
            Page lp = LastPage;

            if (lp == null)             // if no last page nothing to do
                return;

            _pages.RemoveAt(_pages.Count - 1);  // remove the page

            if (this.CurrentPage == lp) // reset the current if necessary
            {
                if (_pages.Count <= 0)
                    CurrentPage = null;
                else
                    CurrentPage = _pages[_pages.Count - 1];
            }

            return;
        }

        public Graphics G
        {
            get
            {
                if (_g == null)
                {
                    _bm = new Bitmap(10, 10);   // create a small bitmap to base our graphics
                    _g = Graphics.FromImage(_bm);
                }
                return _g;
            }
        }

        public int PageCount
        {
            get { return _pages.Count; }
        }

        #region IEnumerable Members

        public IEnumerator GetEnumerator()      // just loop thru the pages
        {
            return _pages.GetEnumerator();
        }

        public void Dispose()
        {
            foreach (var page in _pages)
            {
                page.Dispose();
            }
            CleanUp();
            _pages.Clear();
            _pages = null;
            _report.Dispose();
        }

        #endregion
    }
}
