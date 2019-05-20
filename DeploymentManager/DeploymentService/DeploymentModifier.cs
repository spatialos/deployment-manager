using Improbable.SpatialOS.Deployment.V1Alpha1;
using System.Collections.Generic;
using System.Linq;
using Improbable.SpatialOS.Platform.Common;
using Improbable.SpatialOS.Snapshot.V1Alpha1;
using System.Security.Cryptography;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using Improbable.Worker;
using Deployment = Improbable.SpatialOS.Deployment.V1Alpha1.Deployment;
using Snapshot = Improbable.SpatialOS.Snapshot.V1Alpha1.Snapshot;

namespace DeploymentManager
{
    public static class DeploymentModifier
    {
        private static DeploymentServiceClient deploymentServiceClient;
        private static SnapshotServiceClient snapshotServiceClient; 

        public static void Init(string serviceAccountToken)
        {
            var credentials = new PlatformRefreshTokenCredential(serviceAccountToken);
            deploymentServiceClient = DeploymentServiceClient.Create(credentials: credentials);
            snapshotServiceClient = SnapshotServiceClient.Create(credentials: credentials);
        }
        
        public static GetDeploymentRequest GetGetDeploymentRequest(string deploymentId, string projectName)
        {
            return new GetDeploymentRequest
            {
                Id = deploymentId,
                ProjectName = projectName,
            };
        }
                
        public static ListDeploymentsRequest GetListDeploymentRequest(string deploymentName, string projectName)
        {
            return new ListDeploymentsRequest
            {
                DeploymentName = deploymentName,
                ProjectName = projectName,
            };
        }

        public static string UploadSnapshot(string projectName, string deploymentName)
        {
            // Read snapshot.
            var snapshotBytes = Utils.GetByteContent(DeploymentManager.WorkerType, "default.snapshot");

            // Create HTTP endpoint to upload to.
            Snapshot snapshotToUpload;
            using (var md5 = MD5.Create())
            {
                snapshotToUpload = new Snapshot
                {
                    ProjectName = projectName,
                    DeploymentName = deploymentName,
                    Checksum = Convert.ToBase64String(md5.ComputeHash(snapshotBytes)),
                    Size = snapshotBytes.Length
                };
            }

            var snapshotResponse = ExceptionHandler.HandleGrpcCall(() =>
            {

                var uploadSnapshotResponse =
                    snapshotServiceClient.UploadSnapshot(new UploadSnapshotRequest {Snapshot = snapshotToUpload});
                return uploadSnapshotResponse;
            });

            ExceptionHandler.HandleWebRequest(() =>
            {
                // Upload content.
                var httpRequest = WebRequest.Create(snapshotResponse.UploadUrl) as HttpWebRequest;
                httpRequest.Method = "PUT";
                httpRequest.ContentLength = snapshotToUpload.Size;
                httpRequest.Headers.Set("Content-MD5", snapshotToUpload.Checksum);

                using (var dataStream = httpRequest.GetRequestStream())
                {
                    dataStream.Write(snapshotBytes, 0, snapshotBytes.Length);
                }

                // Block until we have a response.
                httpRequest.GetResponse();
            });

            return ExceptionHandler.HandleGrpcCall(() =>
            {
                // Confirm that the snapshot was uploaded successfully.
                var confirmUploadResponse = snapshotServiceClient.ConfirmUpload(new ConfirmUploadRequest
                {
                    DeploymentName = snapshotToUpload.DeploymentName,
                    Id = snapshotResponse.Snapshot.Id,
                    ProjectName = snapshotToUpload.ProjectName
                });

                return confirmUploadResponse.Snapshot.Id;
            });
        }

        public static Deployment GetDeployment(GetDeploymentRequest request)
        {
            return ExceptionHandler.HandleGrpcCall(() =>
            {
                var response = deploymentServiceClient.GetDeployment(request);
                return response.Deployment;
            });
        }

        public static void UpdateDeployment(Deployment deployment)
        {
            ExceptionHandler.HandleGrpcCall(() =>
            {
                var request = new UpdateDeploymentRequest
                {
                    Deployment = deployment
                };

                deploymentServiceClient.UpdateDeployment(request);
            });
        }

        public static void AddDeploymentTag(GetDeploymentRequest request, string tag)
        {
            ExceptionHandler.HandleGrpcCall(() =>
            {
                var deployment = GetDeployment(request);

                if (!deployment.Tag.Contains(tag))
                {
                    deployment.Tag.Add(tag);
                    UpdateDeployment(deployment);
                }
            });
        }

        public static void RemoveDeploymentTag(GetDeploymentRequest request, string tag)
        {
            ExceptionHandler.HandleGrpcCall(() =>
            {
                var deployment = GetDeployment(request);

                if (deployment.Tag.Contains(tag))
                {
                    deployment.Tag.Remove(tag);
                    UpdateDeployment(deployment);
                }
            });
        }

        public static void UpdateDeploymentTag(GetDeploymentRequest request, string tagPrefix, string tagValue)
        {
            ExceptionHandler.HandleGrpcCall(() =>
            {
                var deployment = GetDeployment(request);
                var tags = deployment.Tag.ToList();
                foreach (var tag in tags)
                {
                    if (tag.StartsWith(tagPrefix))
                    {
                        deployment.Tag.Remove(tag);
                    }
                }
                deployment.Tag.Add($"{tagPrefix}_{tagValue}");
                UpdateDeployment(deployment);
            });
        }

        public static Google.LongRunning.Operation<Deployment, CreateDeploymentMetadata> CreateDeployment(DeploymentTemplate template)
        {
            var request = new CreateDeploymentRequest
            {
                Deployment = template.ToDeployment()
            };

            return ExceptionHandler.HandleGrpcCall(() => deploymentServiceClient.CreateDeployment(request));
        }

        public static void StopDeployment(Deployment deployment)
        {
            ExceptionHandler.HandleGrpcCall(() =>
            {
                var request = new StopDeploymentRequest
                {
                    Id = deployment.Id,
                    ProjectName = deployment.ProjectName,
                };

                deploymentServiceClient.StopDeployment(request);
            });
        }

        public static List<Deployment> ListDeployments(ListDeploymentsRequest request)
        {
            return ExceptionHandler.HandleGrpcCall(() =>
            {
                var response = deploymentServiceClient.ListDeployments(request);
                return response.ToList();
            });
        }
        
        public static void SetMaxWorkerCapacity(GetDeploymentRequest request, string workerType, int maxWorkerCapacity)
        {
            ExceptionHandler.HandleGrpcCall(() =>
            {
                var deployment = GetDeployment(request);
                foreach (var workerCapacity in deployment.WorkerConnectionCapacities)
                {
                    if (workerCapacity.WorkerType == workerType)
                    {
                        workerCapacity.MaxCapacity = maxWorkerCapacity;
                    }
                }

                UpdateDeployment(deployment);
            });
        }

        public static int GetMaxWorkerCapacity(GetDeploymentRequest request, string workerType)
        {
            return ExceptionHandler.HandleGrpcCall(() =>
            {
                var deployment = GetDeployment(request);
                foreach (var workerCapacity in deployment.WorkerConnectionCapacities)
                {
                    if (workerCapacity.WorkerType == workerType)
                    {
                        return workerCapacity.MaxCapacity;
                    }
                }

                throw new ArgumentException($"Couldn't find worker type {workerType} in deployment {deployment.Name}");
            });
        }

        public static int GetRemainingWorkerCapacity(GetDeploymentRequest request, string workerType)
        {
            return ExceptionHandler.HandleGrpcCall(() =>
            {
                var deployment = GetDeployment(request);
                foreach (var workerCapacity in deployment.WorkerConnectionCapacities)
                {
                    if (workerCapacity.WorkerType == workerType)
                    {
                        return workerCapacity.RemainingCapacity;
                    }
                }

                throw new ArgumentException($"Couldn't find worker type {workerType} in deployment {deployment.Name}");
            });
        }
        
        public static int GetCurrentWorkerCapacity(GetDeploymentRequest request, string workerType)
        {
            return ExceptionHandler.HandleGrpcCall(() =>
            {
                var deployment = GetDeployment(request);
                foreach (var workerCapacity in deployment.WorkerConnectionCapacities)
                {
                    if (workerCapacity.WorkerType == workerType)
                    {
                        return workerCapacity.MaxCapacity - workerCapacity.RemainingCapacity;
                    }
                }

                throw new ArgumentException($"Couldn't find worker type {workerType} in deployment {deployment.Name}");
            });
        }
    }
}
