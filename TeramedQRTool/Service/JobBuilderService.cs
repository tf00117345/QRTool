using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Dicom;
using Dicom.Media;
using NLog;
using TeramedQRTool.Utility;

namespace TeramedQRTool.Service
{
    public class JobBuilderService : IDisposable
    {
        private readonly string _cdViewerPath;
        private readonly string _videoPreviewJpg;
        private readonly Logger logger = LogManager.GetCurrentClassLogger();

        public JobBuilderService()
        {
            _cdViewerPath = ConfigService.GetInstance().GetSetting().OtherSetting.DefaultCDViewerPath;
            _videoPreviewJpg = Path.Combine(_cdViewerPath, "videoPreview.jpg");
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public async Task BuildDicomDirAsync(IEnumerable<string> images, string exportDirName)
        {
            // disk path
            var path = ConfigService.JobStoragePath;
            path = Path.Combine(path, exportDirName.Trim());
            DirectoryHelper.CreateDirectory(path);
            DirectoryHelper.CreateDirectory(Path.Combine(path, "IMG"));
            DirectoryHelper.CreateDirectory(Path.Combine(path, "VIDEO"));
            var dicomDirPath = Path.Combine(path, "DICOMDIR");
            var dicomDir = new DicomDirectory();

            var idx = 1;
            try
            {
                foreach (var imageFilePath in images)
                {
                    var referencedFileId = $@"IMG\IMG{Convert.ToString(idx)}";
                    var dicomFile = await DicomFile.OpenAsync(imageFilePath);

                    // Video
                    if (dicomFile.Dataset.InternalTransferSyntax == DicomTransferSyntax.MPEG4AVCH264HighProfileLevel41)
                    {
                        logger.Trace($"video_{idx}: {imageFilePath}");
                        var fileName = $@"VIDEO\VIDEO{Convert.ToString(idx)}";
                        var videoPath = Path.Combine(path, fileName + ".mp4");
                        BuildMP4(dicomFile.Dataset, videoPath);
                        if (File.Exists(_videoPreviewJpg))
                            DirectoryHelper.Copy(_videoPreviewJpg, Path.Combine(path, fileName + ".jpg"));
                    }
                    // Dcm
                    else
                    {
                        logger.Trace($"image_{idx}: {imageFilePath}");
                        dicomDir.AddFile(dicomFile, referencedFileId);
                        var targetPath = Path.Combine(path, referencedFileId);
                        DirectoryHelper.Copy(imageFilePath, targetPath);
                    }

                    idx++;
                }

                DirectoryHelper.CopyAllFileInFolder(_cdViewerPath, path + "\\");
                await dicomDir.SaveAsync(dicomDirPath);
            }
            catch (Exception e)
            {
                logger.Error($"Build dicomdir failed, study, reason: {e.Message}");
                throw;
            }
        }

        private void BuildMP4(DicomDataset dataset, string videoFilePath)
        {
            var item = dataset.GetDicomItem<DicomItem>(DicomTag.PixelData);
            if (!(item is DicomOtherByteFragment fragment)) return;
            var buffer = fragment.Fragments[0];
            File.WriteAllBytes(videoFilePath, buffer.Data);
        }
    }
}