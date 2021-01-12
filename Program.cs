using Ionic.Zip;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleFile
{
    class Program
    {

        static void Main(string[] args)
        {
            using (var zip = new ZipFile())
            {
                try
                {
                    List<LogRetrieveDto> listDto = new List<LogRetrieveDto>();
                    LogRetrieveDto logRetrieveDto1 = new LogRetrieveDto();
                    logRetrieveDto1.Type = SensitiveType.Ip;
                    logRetrieveDto1.OldString = @"[a-za-z0-9][-a-za-z0-9]{0,62}(\.[a-za-z0-9][-a-za-z0-9]{0,62})+\\jqhuang";
                    logRetrieveDto1.NewString = @"[a-zA-z]+://[^\s]*2";
                    logRetrieveDto1.IsWildCard = true;
                    LogRetrieveDto logRetrieveDto2 = new LogRetrieveDto();
                    logRetrieveDto2.Type = SensitiveType.Ip;
                    logRetrieveDto2.OldString = @"[a-za-z0-9][-a-za-z0-9]{0,62}(\.[a-za-z0-9][-a-za-z0-9]{0,62})+\\dylin";
                    logRetrieveDto2.NewString = @"=========";
                    logRetrieveDto2.IsWildCard = true;
                    LogRetrieveDto logRetrieveDto3 = new LogRetrieveDto();
                    logRetrieveDto3.Type = SensitiveType.Ip;
                    logRetrieveDto3.OldString = @"[a-za-z]+://[^\s]*csd_config";
                    logRetrieveDto3.NewString = @"=========";
                    logRetrieveDto3.IsWildCard = true;
                    LogRetrieveDto logRetrieveDto4 = new LogRetrieveDto();
                    logRetrieveDto4.Type = SensitiveType.Ip;
                    logRetrieveDto4.OldString = @"[a-za-z]+://[^\s]*site2";
                    logRetrieveDto4.NewString = @"========";
                    logRetrieveDto4.IsWildCard = false;
                    listDto.Add(logRetrieveDto1);
                    listDto.Add(logRetrieveDto2);
                    listDto.Add(logRetrieveDto3);
                    listDto.Add(logRetrieveDto4);
                    //var file = @"C:\Code\DocAve_6.12.1_6003_AvePoint_Branch\server\VCControl\VCControlWeb\Control.Web.Html\Logs\DocAve-Control.1.log";
                    //new LogFileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, listDto);
                    //var logsFolder = @"C:\Code\DocAve_6.12.1_6003_AvePoint_Branch\server\VCControl\VCControlWeb\Control.Web.Html\Logs";
                    //var logsFolder = @"C:\Code\DocAve_6.12.1_6003_AvePoint_Branch\server\VCControl\VCControlWeb\Control.Web.Html\Logs1";
                    //var logsFolder = @"C:\Code\DocAve_6.12.1_6003_AvePoint_Branch\server\VCControl\VCControlWeb\Control.Web.Html\logs";
                    var logsFolder = @"C:\codeCopy\file";
                    //var logsFolder = @"F:\codes\Newfolder";
                    Console.WriteLine("start >>> " + DateTime.Now);
                    List<LogRetrieveDto> _listDto = new List<LogRetrieveDto>();
                    foreach (LogRetrieveDto logRetrieveDto in listDto)
                    {
                        // 是否是正则查询
                        if (logRetrieveDto.IsWildCard)
                        {
                            _listDto.Add(new LogRetrieveDto() { OldString = logRetrieveDto.OldString.ToLower(CultureInfo.CurrentCulture), NewString = logRetrieveDto.NewString, IsWildCard = logRetrieveDto.IsWildCard });
                        }
                        else
                        {
                            if (logRetrieveDto.OldString.Contains("\\"))
                            {
                                string tmpstr = logRetrieveDto.OldString.Replace('\\', '#');
                                _listDto.Add(new LogRetrieveDto() { OldString = tmpstr.ToLower(CultureInfo.CurrentCulture), NewString = logRetrieveDto.NewString, IsWildCard = logRetrieveDto.IsWildCard });
                            }
                            _listDto.Add(new LogRetrieveDto() { OldString = logRetrieveDto.OldString.ToLower(CultureInfo.CurrentCulture), NewString = logRetrieveDto.NewString, IsWildCard = logRetrieveDto.IsWildCard });
                        }
                    }
                    AddFileOrFolderToEntry(logsFolder, logsFolder, zip, _listDto, ((file) => { return true; }));

                    if (zip.Entries.Count > 65000)
                    {
                        zip.UseZip64WhenSaving = Zip64Option.AsNecessary;
                    }
                    Stopwatch watch = new Stopwatch();
                    watch.Start();
                    zip.Save(@"C:\codeCopy\" + DateTime.Now.Ticks + ".zip");
                    Console.WriteLine("end >>> " + DateTime.Now);
                    watch.Stop();
                    Console.ReadLine();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                    throw;
                }
            }
        }
        private static void AddFileOrFolderToEntry(string baseFolder, string logsFolder, ZipFile zip, List<LogRetrieveDto> listDto, Func<string, bool> fileNeedScrub)
        {
            foreach (string file in Directory.GetFiles(logsFolder))
            {
                //string file = Directory.GetFiles(logsFolder)[i];
                // Console.WriteLine("zip >>> {0}   logsFolder >>> {1}   file >>> {2}   listDto >>> {3}", zip, logsFolder, file, listDto);
                //if (new FileInfo(file).Length > 1024*1024*10)
                //{
                //    ThreadMethodHelper arg = new ThreadMethodHelper { Zip = zip, BaseFolder = baseFolder, LogsFolder = logsFolder, FileName = file, ListDto = listDto };
                //    ParameterizedThreadStart ts = new ParameterizedThreadStart(ProcessData);
                //    Thread th = new Thread(ts, 10);
                //    th.Start(arg);
                //}
                //else
                //{
                //FileInfo info = new FileInfo(file);
                //var  _length = info.Length;
                //Console.WriteLine("info.Length : {0}", info.Length);
                //if (_length >1021*1024*5)
                //{

                    zip.AddEntry(GetEntryName(baseFolder, file), new LogFileStreamF(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, listDto));
                //}
                //else
                //{
                //    zip.AddEntry(GetEntryName(baseFolder, file), new LogFileStreamH(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, listDto));
                //}
                
                //}


            }
            foreach (string folder in Directory.GetDirectories(logsFolder))
            {
                if (Directory.GetFiles(folder).Length + Directory.GetDirectories(folder).Length == 0)
                {
                    zip.AddDirectory(folder, GetEntryName(baseFolder, folder));
                    //ThreadMethodHelper arg = new ThreadMethodHelper { Zip = zip, BaseFolder = baseFolder, LogsFolder = logsFolder, FileName  = file, ListDto = listDto };
                    //ParameterizedThreadStart ts = new ParameterizedThreadStart(ProcessData);
                    //Thread th = new Thread(ts);
                    //th.Start(arg);
                }
                else
                {
                    AddFileOrFolderToEntry(baseFolder, folder, zip, listDto, fileNeedScrub);
                }
            }
           // Console.WriteLine("Thread >>> " + Thread.CurrentThread.ManagedThreadId);
        }
        private static string GetEntryName(string baseFolder, string fileorfolder)
        {
            return fileorfolder.Substring(baseFolder.Length + 1); ;
        }

    }
}
