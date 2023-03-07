using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Timers;
using Dicom;
using TeramedQRTool.Service;
using TeramedQRTool.Utility;
using NLog;
using TeramedQRTool.Helper;
using TeramedQRTool.Model.AppConfig;

namespace TeramedQRTool.Runner
{
    public class ArchivingRunner : Runner
    {
        private static ArchivingRunner _instance;
        private readonly ConcurrentQueue<List<string>> _queue;
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly JobBuilderService _jobBuilder = new JobBuilderService();

        private readonly string _nameOfExportDir =
            ConfigService.GetInstance().GetSetting().OtherSetting.NameOfExportDir;

        private ArchivingRunner()
        {
            _queue = new ConcurrentQueue<List<string>>();
        }

        public static ArchivingRunner GetInstance()
        {
            return _instance ?? (_instance = new ArchivingRunner());
        }

        public void RegisterStudy(List<string> dicomFilePathList)
        {
            _queue.Enqueue(dicomFilePathList);
        }

        protected override void OnTimedEvent(object sender, ElapsedEventArgs e)
        {
            Stop();

            if (_queue.Count == 0)
            {
                Start();
                return;
            }

            _queue.TryDequeue(out var imageList);
            {
                if (imageList.Count == 0)
                    return;

                var directory = new DirectoryInfo(Path.GetDirectoryName(imageList[0]));
                var guid = directory.Name;
                var patId = directory?.Parent?.Name;
                var studyDir = Path.Combine(ConfigService.TempStoragePath, guid);

                try
                {
                    // 一個連線一個DICOMDIR
                    var dcm = DicomFile.Open(imageList[0]);
                    var exportDirName = StringParameterParser.Parse(_nameOfExportDir,
                        (dicomTag) =>
                        {
                            var group = Convert.ToInt32(dicomTag.Split(',')[0], 16);
                            var element = Convert.ToInt32(dicomTag.Split(',')[1], 16);
                            dcm.Dataset.TryGetString(new DicomTag((ushort)group, (ushort)element), out var defaultStr);
                            return defaultStr;
                        });

                    _jobBuilder.BuildDicomDirAsync(imageList, exportDirName);
                }
                catch (Exception exception)
                {
                    var errorPath = Path.Combine(ConfigService.ErrorPath, patId);
                    DirectoryHelper.MoveDir(studyDir, errorPath);
                    _logger.Error($"Archive error: {exception.Message}, move file to: {errorPath}");
                }
                finally
                {
                    if (Directory.Exists(studyDir)) DirectoryHelper.DeleteDir(studyDir);
                }
            }
            Start();
        }
    }
}