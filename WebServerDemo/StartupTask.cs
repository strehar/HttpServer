#region Licence
/*
   Copyright 2016 Miha Strehar

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/
#endregion

using Windows.ApplicationModel.Background;
using Windows.Foundation;
using Windows.System.Threading;

// The Background Application template is documented at http://go.microsoft.com/fwlink/?LinkID=533884&clcid=0x409

namespace WebServerDemo
{
    /// <summary>
    /// Startup for our demo project.
    /// </summary>
    public sealed class StartupTask : IBackgroundTask
    {
        BackgroundTaskDeferral _serviceDeferral;
        StartDemo server;
        public void Run(IBackgroundTaskInstance taskInstance)
        {
            taskInstance.Canceled += OnCanceled;
            _serviceDeferral = taskInstance.GetDeferral();
            server = new StartDemo();
            IAsyncAction asyncAction = ThreadPool.RunAsync((workItem) =>
            {
                server.Start();
            });
        }

        private void OnCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            _serviceDeferral.Complete();
        }
    }
}
