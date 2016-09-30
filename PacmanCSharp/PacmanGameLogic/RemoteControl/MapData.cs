using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Pacman.GameLogic.RemoteControl
{
    public class RPCData
    {
        public MapData MapData;
        public int GamesToPlay;
        public string AIToUse;
        public int MaxScore;
        public int MinScore;
        public bool Parallel;
        public int RandomSeed;
    }

    [Serializable]
    [DataContractAttribute(IsReference = true)]
    public class MapData
    {
        [DataMember]
        public List<double> EvolvedValues;

        public MapData()
        {
            EvolvedValues = new List<double>();
        }

        public MapData(List<double> AttackV)
        {
            EvolvedValues = new List<double>();
            if(AttackV != null)
                EvolvedValues.AddRange(AttackV);
        }

        public MapData Clone()
        {
            return new MapData(EvolvedValues);
        }
    }
}
