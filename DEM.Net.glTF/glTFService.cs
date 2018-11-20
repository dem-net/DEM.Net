using AssetGenerator;
using AssetGenerator.Runtime;
using DEM.Net.Lib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LineAtlas.Core.glTF
{
    public class glTFService : IglTFService
    {
        public void Export(Model model, string outputFolder, string modelName)
        {
            try
            {
                Stopwatch.StartNew();

                Directory.CreateDirectory(outputFolder);
                var jsonSerializer = new Newtonsoft.Json.JsonSerializer
                {
                    Formatting = Newtonsoft.Json.Formatting.Indented,
                    ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
                };


                var filename = $"{modelName}.gltf";

                using (var data = new Data($"{modelName}.bin"))
                {
                    // Passes the desired properties to the runtime layer, which then coverts that data into
                    // a gltf loader object, ready to create the model
                    var converter = new GLTFConverter { CreateInstanceOverride = model.CreateSchemaInstance };
                    var gltf = converter.ConvertRuntimeToSchema(model.GLTF, data);

                    // Makes last second changes to the model that bypass the runtime layer
                    // in order to add features that don't really exist otherwise
                    model.PostRuntimeChanges(gltf);

                    // Creates the .gltf file and writes the model's data to it
                    var assetFile = Path.Combine(outputFolder, filename);
                    glTFLoader.Interface.SaveModel(gltf, assetFile);

                    // Creates the .bin file and writes the model's data to it
                    var dataFile = Path.Combine(outputFolder, data.Name);
                    File.WriteAllBytes(dataFile, data.ToArray());
                }


                Console.WriteLine("Model Creation Complete!");
                Console.WriteLine("Completed in : " + TimeSpan.FromTicks(Stopwatch.GetTimestamp()).ToString());
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
                throw;
            }
        }

    }
}
