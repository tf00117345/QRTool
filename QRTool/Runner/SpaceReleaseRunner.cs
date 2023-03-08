using System;
using System.IO;
using System.Linq;
using System.Timers;
using TeramedQRTool.Model.AppConfig;
using TeramedQRTool.Model.Repository;
using TeramedQRTool.Service;
using TeramedQRTool.Utility;
using RepoDb;
using RepoDb.Enumerations;

namespace TeramedQRTool.Runner
{
    public class SpaceReleaseRunner
    {
        private static Timer _timer;
        private readonly Config _config = ConfigService.GetInstance().GetSetting();
        private readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public SpaceReleaseRunner()
        {
            ReleaseSpace();
            ScheduleTimer();
        }

        public void ScheduleTimer()
        {
            var nowTime = DateTime.Now;
            var scheduledTime =
                new DateTime(nowTime.Year, nowTime.Month, nowTime.Day, 12, 0, 0,
                    0);
            if (nowTime > scheduledTime) scheduledTime = scheduledTime.AddDays(1);

            var tickTime = (scheduledTime - DateTime.Now).TotalMilliseconds;
            _timer = new Timer(tickTime);
            _timer.Elapsed += Timer_Elapsed;
            _timer.Start();
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _timer.Stop();
            ReleaseSpace();
            ScheduleTimer();
        }

        private void ReleaseSpace()
        {
            try
            {

            }
            catch (Exception e)
            {
                logger.Error($"Release space error: {e.Message}");
            }
        }
    }
}