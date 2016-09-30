using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Security;
using System.Web;
using System.Drawing;

using Pacman.GameLogic;

namespace PacmanAI
{
    public class Utility
    {
        //[field: NonSerializedAttribute()]
        public static Image m_GreenBlock = Image.FromFile("green_block.png");
        public static Image m_RedBlock = Image.FromFile("red_block.png");
        public static Image m_BlueBlock = Image.FromFile("blue_block.png");
        
    }
}
