using Cca.Cgp.Core.Base.Interfaces;
using Cca.Cgp.Core.ZmqConductor;
using Cca.Extensions.Common.Logging;
using Cca.Extensions.Common.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using UAManagedCore;

namespace NetZero.SO
{
    internal class Base
    {
        #region Private Fields

        private Process soCGP;

        //
        // default values to use with no command line options
        private static readonly Dictionary<string, object> _defaultOptions = new Dictionary<string, object>
        {
            { "name", "ftoptix" },
            { "ip", "127.0.0.1" },
            { "port", 55000},
            { "loggingLevel", 1 }
        };

        // for the Main() to know if we are exiting
        private static bool _exiting = false;

        // to ensure we only handle shutdown events one at a time
        private static readonly object _lock = new object();

        // the Concerto
        private static IConcerto? s_Concerto;

        #endregion Private Fields

        #region Public Methods

        public Base(IUANode owner)
        {
            Log.Info("Smart Objects", "Starting");

            try
            {
                var coreDirectory = owner.Owner.GetVariable("UtilityFolder").Value.Value.ToString().ToLower();
                var localMachineIP = owner.Owner.GetVariable("NIC").Value.Value.ToString();
                var port = owner.Owner.GetVariable("Port").Value.Value.ToString();

                foreach (var process in Process.GetProcessesByName("so"))
                {
                    process.Kill();
                }

                soCGP = new Process();
                soCGP.StartInfo.CreateNoWindow = false;
                soCGP.StartInfo.UseShellExecute = false;
                soCGP.StartInfo.FileName = Path.Combine(coreDirectory, "so.exe");
                soCGP.StartInfo.WorkingDirectory = coreDirectory;
                soCGP.StartInfo.Arguments = $"-port {port} -cdaIP \"{localMachineIP}\"";
                soCGP.ErrorDataReceived += Cgp_ErrorDataReceived;
                soCGP.Exited += Cgp_Exited;

                soCGP.Start();

                while (soCGP.Id == 0)
                {
                    Thread.Yield();
                }

                CreateConcerto();

                SOConcerto.Instance.ProcessControllerConfigurations(owner, coreDirectory);
            }
            catch (Exception ex)
            {
                Log.Error("Smart Objects", ex.Message);
            }
        }

        public void Stop()
        {
            s_Concerto?.Dispose();
            soCGP.Kill();
        }

        #endregion Public Methods

        #region Private Methods

        private void Cgp_Exited(object? sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private void Cgp_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void CreateConcerto()
        {
            CommonConfig.Load(_defaultOptions);

#if DEBUG
            CommonConfig.Options["loggingLevel"] = 1;
            Logging.SetupStaticLogger(CommonConfig.Options, debug: true);
#else
            CommonConfig.Options["loggingLevel"] = 1;
            Logging.SetupStaticLogger(CommonConfig.Options);
#endif
            // concerto instance
            s_Concerto = SOConcerto.Instance;
            // use the same comms interface for actions and reactions
            var audience = new ZmqConcertoComms(CommonConfig.Options);
            // use the same instance for both adapter and application comms
            var instrument = audience;
            // initialize concerto with comms and start it
            s_Concerto.Start(CommonConfig.Options, audience, instrument, false).ConfigureAwait(false);
        }

        #endregion Private Methods
    }
}
