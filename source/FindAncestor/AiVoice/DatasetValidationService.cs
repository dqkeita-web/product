// Voice/AiVoice/DatasetValidationService.cs
using System;
using System.IO;
using System.Linq;
using FindAncestor.ErrorDialog;
namespace FindAncestor.AiVoice
{ 
    public class DatasetValidationService { public bool Validate(string dir)
        { 

            try { 
                if (!Directory.Exists(dir)) 
                { 
                    ErrorDialogHelper.Show("datasetなし"); return false;

                } 
                var f = Directory.GetFiles(dir, "*.wav"); 
                if (f.Length < 10) {
                    ErrorDialogHelper.Show("データ不足"); return false;
                } return true; 
            } 
            catch (Exception ex)
            { 
                ErrorDialogService.Show(ex); return false;
            } 
        }
    } 
}