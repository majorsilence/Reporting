/* ====================================================================
   Copyright (C) 2004-2008  fyiReporting Software, LLC
   Copyright (C) 2011  Peter Gill <peter@majorsilence.com>

   This file is part of the fyiReporting RDL project.
	
   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at 

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.


   For additional information, email info@fyireporting.com or visit
   the website www.fyiReporting.com.
*/
// File derived from Douglas-Peucker line simplification algorithm implementation by Jack Snoeyink
//   http://www.cs.sunysb.edu/~algorith/implement/DPsimp/implement.shtml

using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;

namespace Majorsilence.Reporting.RdlMapFile
{
    public class DPSimp
    {
/*  				1-26-94      Jack Snoeyink
Non-recursive implementation of the Douglas Peucker line simplification
algorithm.
*/

        Stack<int> stack = new Stack<int>();         // recursion stack
        Point[] V;
        List<Point> R = new List<Point>();
        bool outFlag = true;

        internal DPSimp(Point[] vec)
        {
            V = vec;
        }

        internal Point[] GetDouglasPeuckerSimplified()
        {
            R.Clear();      // clear out result
            
            DPbasic(0, V.Length - 2);       // Assuming first and last point the same (closed polygon)

            // Make sure we close the polygon
            if (!(R[0].X == R[R.Count - 1].X &&
                R[0].Y == R[R.Count - 1].Y))
                R.Add(R[0]);

            Point[] result = R.ToArray();
            R.Clear();

            return result;
        }

        class HOMOG 
        { 
            public int X=0; 
            public int Y=0; 
            public int W=0;

            new public string ToString()
            {
                return string.Format("X={0}; Y={1}; W={2};", X, Y, W);
            }
        }

        void Find_Split(int i, int j, out int split, out float dist) /* linear search for farthest point */
        {
            int k;
            HOMOG q= new HOMOG();
            float tmp;
            split = i;      // just to force a value
            dist = -1;
            if (i + 1 < j)
            {
                CROSSPROD_2CCH(V[i], V[j], q); /* out of loop portion */
                /* of distance computation */
                for (k = i + 1; k < j; k++)
                {
                    tmp = DOTPROD_2CH(V[k], q); /* distance computation */
                    if (tmp < 0) tmp = -tmp; /* calling fabs() slows us down */
                    if (tmp > dist)
                    {
                        dist = tmp;	/* record the maximum */
                        split = k;
                    }
                }
                dist *= dist / (q.X * q.X + q.Y * q.Y); /* correction for segment */
            }				   /* length---should be redone if can == 0 */
        }

        void CROSSPROD_2CCH(Point p, Point q,  HOMOG r) /* 2-d cartesian to homog cross product */
        {
            r.W = p.X * q.Y - p.Y * q.X;
            r.X = -q.Y + p.Y;
            r.Y = q.X - p.X;
        }

        float DOTPROD_2CH(Point p, HOMOG q)	/* 2-d cartesian to homog dot product */
        {
            return q.W + p.X * q.X + p.Y * q.Y;
        }

        void DPbasic(int i, int j)		/* Basic DP line simplification */
        {
            int split;
            float dist_sq;

            stack.Clear();
            stack.Push(j);
            do
            {
                Find_Split(i, stack.Peek(), out split, out dist_sq);
                if (dist_sq > 0.0f)
                {
                    stack.Push(split);
                }
                else
                {
                    Output(i, stack.Peek()); /* output segment Vi to Vtop */
                    i = stack.Pop();
                }
            }
            while (stack.Count > 0);
        }

        void Output(int i, int j)
        {
            if (outFlag)
            {
                outFlag = false;
                R.Add(V[i]);
            }
            R.Add(V[j]);
        }

    }
}
