/************************************* Module Header ***********************************\ 
* Module Name:  CreateProcessAsUserWrapper.cs 
* Project:      CSCreateProcessAsUserFromService 
* Copyright (c) Microsoft Corporation. 
*  
* The sample demonstrates how to create/launch a process interactively in the session of  
* the logged-on user from a service application written in C#.Net. 
*  
* This source is subject to the Microsoft Public License. 
* See http://www.microsoft.com/en-us/openness/licenses.aspx#MPL 
* All other rights reserved. 
*  
* THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND,  
* EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED  
* WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE. 
\***************************************************************************************/

using System;
using System.Runtime.InteropServices;
using System.IO;
using System.Diagnostics;

using ASCOM.Wise40.Common;

namespace Wise40Watcher
{
    class CreateProcessAsUserWrapper
    {
        public static void LaunchChildProcess(string ChildProcName, out int pid)
        {
            IntPtr ppSessionInfo = IntPtr.Zero;
            UInt32 SessionCount = 0;
            string workingDir = Path.GetDirectoryName(ChildProcName);

            pid = 0;
            
            if (Kernel32.WTSEnumerateSessions(
                (IntPtr)Kernel32.WTS_CURRENT_SERVER_HANDLE,  // Current RD Session Host Server handle would be zero. 
                0,                                  // This reserved parameter must be zero. 
                1,                                  // The version of the enumeration request must be 1. 
                ref ppSessionInfo,                  // This would point to an array of session info. 
                ref SessionCount                    // This would indicate the length of the above array. 
                ))
            {
                for (int nCount = 0; nCount < SessionCount; nCount++)
                {
                    // Extract each session info and check if it is the  
                    // "Active Session" of the current logged-on user. 
                    Kernel32.WTS_SESSION_INFO tSessionInfo = (Kernel32.WTS_SESSION_INFO)Marshal.PtrToStructure(
                        ppSessionInfo + nCount * Marshal.SizeOf(typeof(Kernel32.WTS_SESSION_INFO)),
                        typeof(Kernel32.WTS_SESSION_INFO)
                        );

                    if (Kernel32.WTS_CONNECTSTATE_CLASS.WTSActive == tSessionInfo.State)
                    {
                        IntPtr hToken = IntPtr.Zero;
                        if (Kernel32.WTSQueryUserToken(tSessionInfo.SessionID, out hToken))
                        {
                            // Launch the child process interactively  
                            // with the token of the logged-on user. 
                            Kernel32.PROCESS_INFORMATION tProcessInfo;
                            Kernel32.STARTUPINFO tStartUpInfo = new Kernel32.STARTUPINFO();
                            tStartUpInfo.cb = Marshal.SizeOf(typeof(Kernel32.STARTUPINFO));

                            bool ChildProcStarted = Kernel32.CreateProcessAsUser(
                                hToken,             // Token of the logged-on user. 
                                ChildProcName,      // Name of the process to be started. 
                                null,               // Any command line arguments to be passed. 
                                IntPtr.Zero,        // Default Process' attributes. 
                                IntPtr.Zero,        // Default Thread's attributes. 
                                false,              // Does NOT inherit parent's handles. 
                                0,                  // No any specific creation flag. 
                                null,               // Default environment path. 
                                workingDir,         // Working directory. 
                                ref tStartUpInfo,   // Process Startup Info.  
                                out tProcessInfo    // Process information to be returned. 
                                );

                            if (ChildProcStarted)
                            {
                                // The child process creation is successful! 
                                pid = tProcessInfo.dwProcessId;

                                // If the child process is created, it can be controlled via the out  
                                // param "tProcessInfo". For now, as we don't want to do any thing  
                                // with the child process, closing the child process' handles  
                                // to prevent the handle leak. 
                                Kernel32.CloseHandle(tProcessInfo.hThread);
                                Kernel32.CloseHandle(tProcessInfo.hProcess);
                            }
                            else
                            {
                                // CreateProcessAsUser failed! 
                                log("LaunchChildProcess: CreateProcessAsUser: {0} failed", ChildProcName);
                            }

                            // Whether child process was created or not, close the token handle  
                            // and break the loop as processing for current active user has been done. 
                            Kernel32.CloseHandle(hToken);
                            break;
                        }
                        else
                        {
                            // WTSQueryUserToken failed! 
                            log("LaunchChildProcess: {0} WTSQueryUserToken failed", ChildProcName);
                        }
                    }
                    else
                    {
                        // This Session is not active! 
                        log("LaunchChildProcess: {0} Session not active", ChildProcName);
                    }
                }

                // Free the memory allocated for the session info array. 
                Kernel32.WTSFreeMemory(ppSessionInfo);
            }
            else
            {
                // WTSEnumerateSessions failed! 
                log("LaunchChildProcess: {0} Session not active", ChildProcName);
            }
        }

        private static void log(string fmt, params object[] o)
        {
            string msg = string.Format(fmt, o);
            string dir = ASCOM.Wise40.Common.Debugger.LogDirectory();

            Directory.CreateDirectory(dir);
            using (var sw = new StreamWriter(dir + "/Wise40Watcher-CreateProcess.txt", true))
            {
                sw.WriteLine(DateTime.UtcNow.ToString("HH:mm:ss.ffff UT ") + msg);
            }
        }
    }
}
