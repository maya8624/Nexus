using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nexus.Application.Dtos
{
    public class FraudPredictionRequest
    {
        // The 28 PCA features from the Kaggle dataset
        public List<double> VFeatures { get; set; } = [];
        public double Amount { get; set; }
    }
}
