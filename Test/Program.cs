using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using SharpObjParser;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            Model model;
            string filename = "Resources/cup.obj";
            using (var stream = new FileStream(filename,FileMode.Open))
            {
                var parser = new ObjFileParser(stream, "cup", "Resources/");
                model = parser.GetModel();
            }
        }
    }
}
