// Editor/BuildBlocker.cs
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.IO;

namespace WG_Casino.Editor
{
    public class BuildBlocker : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;
        
        private const string LICENSE_KEY = "WG_HUNTED_REELS_LICENSED";
        private const string LICENSE_FILE = "wg_license.txt";

        public void OnPreprocessBuild(BuildReport report)
        {
            // Verifica se é uma build de desenvolvimento (opcional)
            bool isDevelopmentBuild = (report.summary.options & BuildOptions.Development) != 0;
            
            // Verifica se o arquivo de licença existe
            bool hasLicenseFile = File.Exists(Path.Combine(Application.dataPath, LICENSE_FILE));
            
            // Verifica se a chave de licença está definida
            bool hasLicenseKey = false;
            var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(
                EditorUserBuildSettings.selectedBuildTargetGroup
            );
            hasLicenseKey = defines.Contains(LICENSE_KEY);
            
            // Se tiver licença, permite o build
            if (hasLicenseFile || hasLicenseKey)
            {
                Debug.Log("[WG_Casino] Licença detectada. Build permitido.");
                return;
            }
            
            string message = "╔════════════════════════════════════════════════════════════════╗\n" +
                             "║                    BUILD BLOQUEADO                              ║\n" +
                             "║                    WG HUNTED REELS                              ║\n" +
                             "╠════════════════════════════════════════════════════════════════╣\n" +
                             "║                                                                ║\n" +
                             "║  Este package contém código proprietário e não pode ser         ║\n" +
                             "║  incluído em builds sem licença comercial.                      ║\n" +
                             "║                                                                ║\n" +
                             "║  PARA OBTER UMA LICENÇA COMERCIAL:                              ║\n" +
                             "║                                                                ║\n" +
                             "║    Email: suportegamebug@gmail.com                              ║\n" +
                             "║    GitHub: https://github.com/wellsouza                        ║\n" +
                             "║                                                                ║\n" +
                             "║  APÓS ADQUIRIR A LICENÇA:                                       ║\n" +
                             "║                                                                ║\n" +
                             "║    1. Remova este arquivo (BuildBlocker.cs) do package         ║\n" +
                             "║    2. Ou adicione a define simbólica:                          ║\n" +
                             "║       WG_HUNTED_REELS_LICENSED                                  ║\n" +
                             "║    3. Ou crie o arquivo: Assets/wg_license.txt                 ║\n" +
                             "║                                                                ║\n" +
                             "╚════════════════════════════════════════════════════════════════╝";

            Debug.LogError(message);
            
            bool continueDialog = EditorUtility.DisplayDialog(
                "Build Bloqueado - WG Hunted Reels",
                "Este package (WG Hunted Reels) é de uso restrito para testes.\n\n" +
                "Para incluir este software em builds, você precisa adquirir uma licença comercial.\n\n" +
                "Deseja continuar mesmo assim? (O build será cancelado de qualquer forma)\n\n" +
                "Contato: suportegamebug@gmail.com",
                "OK, entendi",
                "Cancelar"
            );
            
            throw new BuildFailedException(
                "Build bloqueado: WG Hunted Reels requer licença comercial. " +
                "Entre em contato com suportegamebug@gmail.com"
            );
        }
    }
}