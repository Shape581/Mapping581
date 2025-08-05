using Life;
using Life.DB;
using Life.UI;
using Life.Network;
using Mirror;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Mapping581
{
    public class Main : Plugin
    {
        public string directoryPath;
        public string mappingPath;
        public string configPath;
        public Config config;

        public Main(IGameAPI api) : base(api) { }

        public override void OnPluginInit()
        {
            base.OnPluginInit();
            directoryPath = Path.Combine(pluginsPath, Assembly.GetExecutingAssembly().GetName().Name);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            configPath = Path.Combine(directoryPath, "config.json");
            if (!File.Exists(configPath))
            {
                config = new Config();
                File.WriteAllText(configPath, JsonConvert.SerializeObject(config, Formatting.Indented));
            }
            else
                config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(configPath));
            mappingPath = Path.Combine(directoryPath, "Mappings");
            if (!Directory.Exists(mappingPath))
            {
                Directory.CreateDirectory(mappingPath);
            }
            new SChatCommand("/mapping", new string[] { "/m" }, "Ouvre le menu de mapping", "/mapping", (player, args) =>
            {
                if (player.account.adminLevel >= config.adminLevel)
                {
                    OpenMenu(player);
                }
                else
                    player.Notify("Mapping581", "Vous n'avez pas la permission.", NotificationManager.Type.Error);
            }).Register();
            if (config.deleteAncientSave)
            {
                var saves = Directory.GetFiles(mappingPath, "*.json");
                foreach (var path in saves)
                {
                    if (DateTime.Now < File.GetLastWriteTime(path) + TimeSpan.FromDays(30))
                    {
                        Console.BackgroundColor = ConsoleColor.Blue;
                        Console.WriteLine($"Ancienne sauvgarde supprime : {Path.GetFileName(path)}");
                        Console.ResetColor();
                        File.Delete(path);
                    }
                }
            }
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"{Assembly.GetExecutingAssembly().GetName().Name} initialise !");
            Console.ResetColor();
        }

        public void OpenMenu(Player player)
        {
            var panel = new UIPanel("Mapping581", UIPanel.PanelType.Tab);
            panel.AddButton("Fermer", ui => player.ClosePanel(ui));
            panel.AddButton("Sélectionner", ui => ui.SelectTab());
            panel.AddTabLine("Nettoyer un terrain", ui =>
            {
                OpenInputAreaIdMenu(player);
            });
            panel.AddTabLine("Créer une sauvegarde", ui =>
            {
                OpenInputSaveAreaIdMenu(player);
            });
            panel.AddTabLine("Importer une sauvegarde", ui =>
            {
                OpenSelectSaveMenu(player);
            });
            player.ShowPanelUI(panel);
        }

        public void OpenSelectSaveMenu(Player player)
        {
            var panel = new UIPanel("Importation de sauvegarde", UIPanel.PanelType.TabPrice);
            panel.AddButton("Fermer", ui => player.ClosePanel(ui));
            panel.AddButton("Sélectionner", ui => ui.SelectTab());
            panel.AddButton("Retour", ui => OpenMenu(player));
            var saves = Directory.GetFiles(mappingPath, "*.json");
            foreach (var path in saves)
            {
                var name = Path.GetFileNameWithoutExtension(path);
                var mapping = JsonConvert.DeserializeObject<Mapping>(File.ReadAllText(path));
                panel.AddTabLine(name + "<br>" + "<i>" + mapping.Objects.Count.ToString() + " Objets" + "</i>", "", -1, ui =>
                {
                    player.ClosePanel(ui);
                    player.Notify("Mapping581", $"Importation de la sauvegarde {name} en cours... Cela peux prendre un peu de temps.", NotificationManager.Type.Info);
                    foreach (var obj in mapping.Objects)
                    {
                        try
                        {
                            Nova.a.CreateObject(
                            obj.ObjectId,
                            obj.ModelId,
                            mapping.AreaId,
                            new Vector3(obj.PositionX, obj.PositionY, obj.PositionZ),
                            new Vector3(obj.RotationX, obj.RotationY, obj.RotationZ),
                            obj.Interior,
                            obj.SteamId,
                            obj.Data);
                        }
                        catch
                        {
                            player.Notify("Mapping581", $"Erreur lors de l'importation de l'objet {obj.ObjectId}.", NotificationManager.Type.Error);
                            continue;
                        }
                    }
                    player.Notify("Mapping581", $"La sauvegarde {name} a été importée avec succès.", NotificationManager.Type.Success);
                });
            }
            player.ShowPanelUI(panel);
        }

        public void OpenInputAreaIdMenu(Player player)
        {
            var panel = new UIPanel("Nettoyage de terrain", UIPanel.PanelType.Input);
            panel.SetText("Veuillez définir l'Id du terrain à nettoyer." + "<br>" + $"<color={LifeServer.COLOR_RED}>N'oubliez pas de crée une sauvegarde !</color>");
            panel.SetInputPlaceholder("Id :");
            panel.AddButton("Fermer", ui => player.ClosePanel(ui));
            panel.AddButton("Valider", ui =>
            {
                if (!string.IsNullOrEmpty(ui.inputText) && ui.inputText.Length > 0)
                {
                    if (int.TryParse(ui.inputText, out int areaId))
                    {
                        player.Notify("Mapping581", $"Nettoyage du terrain {areaId} en cours... Cela peux prendre un peu de temps.", NotificationManager.Type.Info);
                        ClearArea(areaId);
                        player.Notify("Mapping581", $"Le terrain {areaId} a été nettoyé avec succès.", NotificationManager.Type.Success);
                        player.ClosePanel(ui);
                    }
                    else
                        player.Notify("Mapping581", "Format invalide.", NotificationManager.Type.Error);
                }
                else
                    player.Notify("Mapping581", "Format invalide.", NotificationManager.Type.Error);

            });
            panel.AddButton("Retour", ui => OpenMenu(player));
            panel.AddButton("Actuel", ui =>
            {
                if ((int)player.setup.areaId > 0)
                {
                    player.Notify("Mapping581", $"Nettoyage du terrain {player.setup.areaId} en cours... Cela peux prendre un peu de temps.", NotificationManager.Type.Info);
                    ClearArea((int)player.setup.areaId);
                    player.Notify("Mapping581", $"Le terrain {player.setup.areaId} a été nettoyé avec succès.", NotificationManager.Type.Success);
                    player.ClosePanel(ui);
                }
                else
                    player.Notify("Mapping581", "Vous n'êtes pas dans un terrain.", NotificationManager.Type.Error);
            });
            player.ShowPanelUI(panel);
        }

        public void OpenInputSaveAreaIdMenu(Player player)
        {
            var panel = new UIPanel("Terrain de la sauvegarde", UIPanel.PanelType.Input);
            panel.SetText("Veuillez définir l'Id du terrain à sauvegarder.");
            panel.SetInputPlaceholder("Id :");
            panel.AddButton("Fermer", ui => player.ClosePanel(ui));
            panel.AddButton("Valider", ui =>
            {
                if (!string.IsNullOrEmpty(ui.inputText) && ui.inputText.Length > 0)
                {
                    if (int.TryParse(ui.inputText, out int areaId))
                    {
                        OpenInputSaveNameMenu(player, areaId);
                    }
                    else
                        player.Notify("Mapping581", "Format invalide.", NotificationManager.Type.Error);
                }
                else
                    player.Notify("Mapping581", "Format invalide.", NotificationManager.Type.Error);
            });
            panel.AddButton("Retour", ui => OpenMenu(player));
            panel.AddButton("Actuel", ui =>
            {
                if ((int)player.setup.areaId > 0)
                {
                    OpenInputSaveNameMenu(player, (int)player.setup.areaId);
                }
                else
                    player.Notify("Mapping581", "Vous n'êtes pas dans un terrain.", NotificationManager.Type.Error);
            });
            player.ShowPanelUI(panel);
        }

        public void OpenInputSaveNameMenu(Player player, int areaId)
        {
            var panel = new UIPanel("Création de la sauvegarde", UIPanel.PanelType.Input);
            panel.SetText("Veuillez définir le nom de la sauvegarde.");
            panel.SetInputPlaceholder("Nom :");
            panel.AddButton("Fermer", ui => player.ClosePanel(ui));
            panel.AddButton("Valider", ui =>
            {
                if (!string.IsNullOrEmpty(ui.inputText))
                {
                    if (IsValidJsonName(ui.inputText))
                    {
                        player.Notify("Mapping581", $"Création de la sauvegarde {ui.inputText} en cours... Cela peux prendre un peu de temps.", NotificationManager.Type.Info);
                        var mappingObjects = CreateSave(areaId);
                        CreateMapping(ui.inputText, areaId, mappingObjects);
                        player.Notify("Mapping581", $"La sauvegarde {ui.inputText} a été créée avec succès.", NotificationManager.Type.Success);
                        player.ClosePanel(ui);
                    }
                    else
                        player.Notify("Mapping581", "Nom de la sauvegarde invalide.", NotificationManager.Type.Error);
                }
                else
                    player.Notify("Mapping581", "Format invalide.", NotificationManager.Type.Error);
            });
            panel.AddButton("Retour", ui => OpenMenu(player));
            player.ShowPanelUI(panel);
        }

        public List<MappingObject> CreateSave(int areaId)
        {
            var area = Nova.a.GetAreaById((uint)areaId);
            var list = new List<MappingObject>();
            foreach (var obj in area.instance.objects.Values.ToList())
            {
                var mappingObject = new MappingObject()
                {
                    ObjectId = obj.objectId,
                    ModelId = JsonConvert.DeserializeObject<Life.InventorySystem.Item.ModelData>(obj.objectVersion).modelId,
                    PositionX = obj.x,
                    PositionY = obj.y,
                    PositionZ = obj.z,
                    RotationX = obj.rotX,
                    RotationY = obj.rotY,
                    RotationZ = obj.rotZ,
                    Interior = obj.isInterior,
                    SteamId = obj.steamId,
                    Data = obj.data
                };
                list.Add(mappingObject);
            }
            return list;
        }

        public void ClearArea(int areaId)
        {
            var area = Nova.a.GetAreaById((uint)areaId);
            foreach (var obj in area.instance.localObjects.Values.ToList())
            {
                obj.SetPositionAndRotation(new UnityEngine.Vector3(0, 0, 0), Quaternion.Euler(0, 0, 0));
            }
            foreach (var obj in area.instance.spawnedObjects.Values.ToList())
            {
                NetworkServer.Destroy(NetworkServer.spawned[obj.netIdentity.netId].gameObject);
                area.instance.spawnedObjects.Remove(obj.netIdentity.netId);
            }
            foreach (var obj in area.instance.objects.Values.ToList())
            {
                LifeDB.RemoveObject(obj.id);
                area.instance.objects.Remove(obj.id);
            }
        }

        public void CreateMapping(string name, int areaId, List<MappingObject> mappingObjects)
        {
            var filePath = Path.Combine(mappingPath, $"{name}.json");
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            File.WriteAllText(filePath, JsonConvert.SerializeObject(new Mapping() 
            { 
                AreaId = areaId, 
                Objects = mappingObjects 
            }, Formatting.Indented));
        }

        public static bool IsValidJsonName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;
            foreach (char c in name)
            {
                if (c == '"' || c == '\r' || c == '\n' || c == '\t' || char.IsControl(c))
                    return false;
            }
            return true;
        }
    }
}
