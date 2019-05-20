using Improbable.SpatialOS.Deployment.V1Alpha1;
using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Net;

namespace DeploymentManager
{
    public static class ExceptionHandler
    {
        private static HashSet<StatusCode> GrpcRetryStatusCodes = new HashSet<StatusCode>
        {
            StatusCode.FailedPrecondition,
            StatusCode.DeadlineExceeded,
        };
        
        private static HashSet<WebExceptionStatus> HttpRetryStatusCodes = new HashSet<WebExceptionStatus>
        {
            WebExceptionStatus.Timeout,
        };
        
        public static void HandleWebRequest(Action httpRequestCall, int retries = 0)
        {
            try
            {
                httpRequestCall();
            }
            catch (WebException e)
            {
                if (retries < Utils.MaxRetries && HttpRetryStatusCodes.Contains(e.Status))
                {
                    HandleWebRequest(httpRequestCall, retries + 1);
                }

                throw e;
            }
        }

        public static void HandleGrpcCall(Action grpcCall, int retries = 0)
        {
            try
            {
                grpcCall();
            }
            catch (RpcException e)
            {
                if (retries < Utils.MaxRetries && GrpcRetryStatusCodes.Contains(e.Status.StatusCode))
                {
                    HandleGrpcCall(grpcCall, retries + 1);
                }

                throw e;
            }
        }
        
        public static TResult HandleGrpcCall<TResult>(Func<TResult> grpcCall, int retries = 0)
        {
            try
            {
                return grpcCall();
            }
            catch (RpcException e)
            {
                if (retries < Utils.MaxRetries && GrpcRetryStatusCodes.Contains(e.Status.StatusCode))
                {
                    return HandleGrpcCall(grpcCall, retries + 1);
                }

                throw e;
            }
        }
    }
}
