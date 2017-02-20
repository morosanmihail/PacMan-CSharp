using Accord.Statistics.Distributions.Univariate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PacmanServer
{
    public class BarebonesNonPacmanGenerator
    {
        public List<double> GenerateScoresFromIndividual(double[] DistribMean, int SampleCount, bool Skewed, bool BiModal)
        {
            UnivariateContinuousDistribution gaussianDist = null; 

            if(Skewed)
            {
                gaussianDist = new SkewNormalDistribution(DistribMean[0], 3000, 7.2);
            } else
            {
                if (BiModal)
                {
                    double[] coef = { 0.5, 0.5 };
                    gaussianDist = new Mixture<NormalDistribution>(coef, new NormalDistribution(DistribMean[0], 2500), new NormalDistribution(DistribMean[1], 2500));
                }
                else
                {
                    gaussianDist = new NormalDistribution(DistribMean[0], 3000);
                }
            }

            return gaussianDist.Generate(SampleCount).ToList();
        }
    }
}
