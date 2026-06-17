

using System;	
using System.IO;
using System.Text;
using System.Threading;
using System.Diagnostics;

namespace Majorsilence.Reporting.RdlDesktop
{
	class ConsoleThread
	{
		RdlDesktop rserver;					// server we're watching
		BackgroundThread bthread;				// background thread

		public ConsoleThread (RdlDesktop rws, BackgroundThread bk)
		{
			rserver = rws;
			bthread = bk;
		}

		public void HandleConsole()
		{
			string input, arg;
			bool bContinue=true;
			Process p;				// work variable
			while(bContinue)
			{
				input = Console.ReadLine().ToLower();
				int iStartArg = input.IndexOf(" ");
				if (iStartArg >= 0)
				{
					arg = input.Substring(iStartArg+1);
					input = input.Substring(0, iStartArg);
				}
				else
					arg = null;

				switch(input)
				{
					case "clearcache":
					case "cc":
						int numFiles = rserver.ReadCache.ClearAll();
						Console.WriteLine("{0:#,##0} read cache files cleared.", numFiles);
						numFiles = rserver.Cache.ClearAll();
						Console.WriteLine("{0:#,##0} cache files deleted.", numFiles);
						Console.Write(">");
						break;
					case "clearstatistics":
					case "cs":
						ConnectionThread.ClearStatistics();
						Console.WriteLine("Statistics cleared.");
						Console.Write(">");
						break;
					case "exit":
					case "x":
						bthread.Continue = false;		// tell background thread to stop
						rserver.Continue = false;		// tell server to stop
						bContinue=false;
						break;
					case "license":
					case "l":
						Console.WriteLine("Copyright (C) 2004-2008  fyiReporting Software, LLC");
						Console.WriteLine("");
						Console.WriteLine("This file is part of the fyiReporting RDL project.");
						Console.WriteLine("");
						Console.WriteLine("Licensed under the Apache License, Version 2.0 (the \"License\");");
						Console.WriteLine("you may not use this file except in compliance with the License.");
						Console.WriteLine("You may obtain a copy of the License at");
						Console.WriteLine("");
						Console.WriteLine("    http://www.apache.org/licenses/LICENSE-2.0");
						Console.WriteLine("");
						Console.WriteLine("Unless required by applicable law or agreed to in writing, software");
						Console.WriteLine("distributed under the License is distributed on an \"AS IS\" BASIS,");
						Console.WriteLine("WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.");
						Console.WriteLine("See the License for the specific language governing permissions and");
						Console.WriteLine("limitations under the License.");
						Console.WriteLine("");

						Console.WriteLine("For additional information visit");
						Console.WriteLine("the website https://github.com/majorsilence/Reporting.");
						Console.Write(">");
						break;
					case "threads":
					case "th":
						p = Process.GetCurrentProcess();
						ProcessThreadCollection threads = p.Threads;   
						foreach (ProcessThread pt in threads)
						{
							Console.WriteLine("Thread: {0} {1} {2}", pt.Id, pt.ThreadState, pt.ThreadState == System.Diagnostics.ThreadState.Wait? pt.WaitReason.ToString(): "");
						}
						Console.Write(">");
						break;
					case "trace":
					case "t":
						if (arg != null)
						{
							try
							{
								int t = Convert.ToInt32(arg);
								rserver.TraceLevel = t;
							}
							catch
							{
								Console.WriteLine("trace argument must be a number.");
							}
						}
						Console.WriteLine("Trace level= {0}", rserver.TraceLevel);
						Console.Write(">");
						break;
					case "statistics":
					case "s":
						p = Process.GetCurrentProcess();
						Console.WriteLine("Server start= {0}", p.StartTime);
						Console.WriteLine("Elapsed time= {0}", DateTime.Now - p.StartTime);
						// output some connection information
						int totalRequests = ConnectionThread.GetRequestCount();
						Console.WriteLine("Total requests= {0:#,##0}", totalRequests);
						Console.WriteLine("Report cache hits= {0:#,##0}", rserver.Cache.CacheHits);
						Console.WriteLine("Count of report URLs cached= {0:#,##0}", rserver.Cache.Count);
						Console.WriteLine("Read cache hits= {0:#,##0}", rserver.ReadCache.CacheHits);
						Console.WriteLine("Count of read cache files= {0:#,##0}", rserver.ReadCache.Count);
						Console.WriteLine("Current connections= {0:#,##0}", ConnectionThread.GetConnectionCount());
						Console.WriteLine("Peak connections= {0:#,##0}", ConnectionThread.GetPeakConnectionCount());
						// Calculate average length of time per request
						if (totalRequests > 0)
						{
							double secs = ConnectionThread.GetTotalRequestTime();
							Console.WriteLine("Average response time in seconds= {0:0.0000}", secs / totalRequests);
						}
						// Get associated process's physical memory usage.
						Console.WriteLine("Memory usage= {0:#,##0}", p.WorkingSet64);
						Console.WriteLine("Peak working set= {0:#,##0}", p.WorkingSet64);
						// Get total processor time for this process.
						Console.WriteLine("Total processor time= " + p.TotalProcessorTime);
						// Get count of files in cache
						Console.Write(">");
						break;

					case "help":
					case "h":
					case "?":
						Console.WriteLine("Commands");
						Console.WriteLine("clearcache (cc): clears out all cache files.");
						Console.WriteLine("clearstatistics (cs): clears out statistic counters.");
						Console.WriteLine("exit (x): stops the server.");
						Console.WriteLine("license (l): shows the license and warranty.");
						Console.WriteLine("statistics (s): output server statistics");
						Console.WriteLine("threads (th): lists the current threads.");
						Console.WriteLine("trace # (t): sets the console error verbosity. If number isn't supplied current trace level is shown.");
						Console.WriteLine("help (?): this output.");
						Console.Write(">");
						break;
					default:
						Console.Write(">");
						break;
				}
			}
			return;
		}
	}
}
