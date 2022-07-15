using System.Runtime.Serialization;

namespace GameTaskGeneratorConsoleApp
{
    [DataContract]
    class GameTaskData
    {
        [DataMember] public int GameClassOrder { get; set; }
        [DataMember] public string Name { get; set; }
        [DataMember] public string Instruction { get; set; }
        [DataMember] public string Filter { get; set; }
        [DataMember] public int TimeLimit { get; set; }
        [DataMember] public int Reward { get; set; }

        public override string ToString()
        {
            return Name + "," + GameClassOrder + "," + TimeLimit + "," + Reward + "," + Filter + "=>" + Instruction.Substring(0, 30);
        }
    }


}
