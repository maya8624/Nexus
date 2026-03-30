using Nexus.Application.Dtos;
using Nexus.Application.Dtos.Requests;
using Nexus.Application.Dtos.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nexus.Application.Interfaces
{
    public interface IFraudDetectionService
    {
        Task<FraudPredictionResponse> CheckTransaction(FraudPredictionRequest request);
    }
}
