using AssetGenerator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LineAtlas.Core.glTF
{
	public interface IglTFService
	{
		void Export(Model model, string directoryName, string modelName);
	}
}
