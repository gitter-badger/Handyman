﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using Slave.Core.Forms;
using Slave.Core.Models;
using Slave.Framework.Components;
using Slave.Framework.Interfaces;

namespace Slave.Core.Helpers {
    public static class PluginManager {
        public static void ListPlugins() {
            using (var wc = new WebClient()) {
                var json = wc.DownloadString(Properties.Settings.Default.DownloadURL + "plugins.json");
                var pckgs = JsonConvert.DeserializeObject<List<Plugin>>(json);
                var form = new Form { Text = "Packages", Size = new Size(400, 600) };
                var lb = new TextBox {
                    AutoSize = true, ScrollBars = ScrollBars.Vertical, WordWrap = true, ReadOnly = true,
                    Multiline = true, SelectedText = ""
                };

                lb.Text = "To install package write \"install <packageName>\"\r\n\r\n";
                lb.Text += "Packages\r\n===============================================\r\n";
                foreach (var p in pckgs) {
                    lb.Text += "Name: " + p.Name + "\r\n";
                    lb.Text += "Author: " + p.Author + "\r\n";
                    lb.Text += "Description: " + p.Description + "\r\n===============================================\r\n\r\n";
                }

                lb.Text += "\r\n\r\n\r\n";

                lb.Size = new Size(form.Size.Width - 10, form.Size.Height);
                lb.Select(0, 0);
                form.Controls.Add(lb);

                form.SizeChanged += (o, e) => { lb.Size = new Size(form.Size.Width - 10, form.Size.Height); };

                form.ShowDialog();
            }
        }

        public static void InstallPlugin(string name) {
            try {
                using (var wc = new WebClient()) {
                    var json = wc.DownloadString(Properties.Settings.Default.DownloadURL + "plugins.json");
                    var pckgs = JsonConvert.DeserializeObject<List<Plugin>>(json);
                    var pckgForInstall = pckgs.SingleOrDefault(x => x.Name == name);
                    if (pckgForInstall != null) {
                        wc.DownloadFile(pckgForInstall.URL, pckgForInstall.Name + ".dll");
                        if (pckgForInstall.HasConfig)
                            wc.DownloadFile(pckgForInstall + ".config", pckgForInstall.Name + ".dll.config");
                    }
                    Launcher.Current.ChangeLauncherText("success :)");
                    return;
                }
            } catch (Exception e) {
                Launcher.Current.ChangeLauncherText("error :(");
            }
        }

        public static List<IMaster> LoadPlugins(out IContainer components) {
            var tools = new List<IMaster>();
            components = new Container();

            // we extract all the IAttributeDefinition implementations 
            foreach (var filename in Directory.GetFiles(Application.StartupPath /* + "\\Plugins" */, "*.dll")) {
                var assembly = System.Reflection.Assembly.LoadFrom(filename);
                foreach (var type in assembly.GetTypes()) {
                    var plugin = type.GetInterface("Slave.Framework.Interfaces.IMaster");
                    if (plugin != null) {
                        var tool = (IMaster)Activator.CreateInstance(type);
                        tool.Initialize();

                        var hotkey = new SystemHotkey(components) {
                            Shortcut = tool.HotKey
                        };
                        hotkey.Pressed += delegate {
                            tool.Execute(null);
                        };
                        tools.Add(tool);
                    }
                }
            }
            return tools;
        }

        public static bool Handle(string alias) {
            var parts = alias.Split(' ');

            if (parts[0] == "install" && parts.Count() == 2) {
                InstallPlugin(parts[1]);
                return true;
            }
            if (parts[0] == "packages") {
                ListPlugins();
                return true;
            }

            return false;
        }
    }
}
