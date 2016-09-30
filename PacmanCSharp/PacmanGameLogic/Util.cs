using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Text;

namespace Pacman.GameLogic
{
	public static class Util
	{
		public static Image LoadImage(string name) {
			System.Reflection.Assembly thisExe = System.Reflection.Assembly.GetExecutingAssembly();
			System.IO.Stream file = thisExe.GetManifestResourceStream("PacmanGameLogic.Resources." + name);
			return Image.FromStream(file);
		}
	}
}
