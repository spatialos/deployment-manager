using System;
using Improbable.SpatialOS.Deployment.V1Alpha1;
using Improbable.Worker;

namespace DeploymentManager
{
    public class SpatialOSReceiveHandler : IDisposable
    {
        private readonly Dispatcher dispatcher = new Dispatcher();
        private readonly Connection connection;
        private readonly GetDeploymentRequest request;

        public SpatialOSReceiveHandler(Connection connection, GetDeploymentRequest request)
        {
            this.connection = connection;
            this.request = request;
            SetupHandlers();
        }

        public void ProcessOps()
        {
            using (var opList = connection.GetOpList(0))
            {
                dispatcher.Process(opList);
            }
        }

        private void SetupHandlers()
        {
            dispatcher.OnLogMessage(OnLogMessage);
            dispatcher.OnAddComponent<Improbable.Session.Session>(OnSessionComponentAdded);
            dispatcher.OnComponentUpdate<Improbable.Session.Session>(OnSessionComponentUpdate);
        }

        private void OnLogMessage(LogMessageOp message)
        {
            Log.Print(message.Level, message.Message, connection);
        }

        private void OnSessionComponentAdded(AddComponentOp<Improbable.Session.Session> componentAddOp)
        {
            Improbable.Session.Session.Data data = (Improbable.Session.Session.Data) componentAddOp.Data;
            UpdateDeploymentStatus(data.Value.status);
        }

        private void OnSessionComponentUpdate(ComponentUpdateOp<Improbable.Session.Session> componentUpdate)
        {
            Improbable.Session.Session.Update update = (Improbable.Session.Session.Update) componentUpdate.Update;
            UpdateDeploymentStatus(update.status.Value);
        }

        private void UpdateDeploymentStatus(Improbable.Session.Status status)
        {
            DeploymentModifier.UpdateDeploymentTag(request, Tags.Status, status.ToString().ToLower());

            if (status == Improbable.Session.Status.STOPPED)
            {
                var deployment = DeploymentModifier.GetDeployment(request);
                DeploymentModifier.StopDeployment(deployment);
            }
        }

        public void Dispose()
        {
            dispatcher.Dispose();
        }
    }
}
