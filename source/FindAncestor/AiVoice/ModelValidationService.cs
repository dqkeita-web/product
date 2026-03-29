using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FindAncestor.AiVoice
{
    public class ModelValidationService
    {
        public bool Validate(string modelDir)
        {
            try
            {
                if (!Directory.Exists(modelDir)) return false;

                var pth = Directory.GetFiles(modelDir, "*.pth", SearchOption.AllDirectories);
                var index = Directory.GetFiles(modelDir, "*.index", SearchOption.AllDirectories);

                return pth.Length > 0 && index.Length > 0;
            }
            catch
            {
                return false;
            }
        }
    }
}