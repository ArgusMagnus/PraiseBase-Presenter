﻿/*
 *   PraiseBase Presenter
 *   The open source lyrics and image projection software for churches
 *
 *   http://praisebase.org
 *
 *   This program is free software; you can redistribute it and/or
 *   modify it under the terms of the GNU General Public License
 *   as published by the Free Software Foundation; either version 2
 *   of the License, or (at your option) any later version.
 *
 *   This program is distributed in the hope that it will be useful,
 *   but WITHOUT ANY WARRANTY; without even the implied warranty of
 *   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *   GNU General Public License for more details.
 *
 *   You should have received a copy of the GNU General Public License
 *   along with this program; if not, write to the Free Software
 *   Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 *
 */

using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using PraiseBase.Presenter.Editor;
using PraiseBase.Presenter.Forms;
using PraiseBase.Presenter.Manager;
using PraiseBase.Presenter.Persistence.Setlists;
using PraiseBase.Presenter.Presenter;
using PraiseBase.Presenter.Properties;

namespace PraiseBase.Presenter
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main(string[] args)
        {
            DateTime startTime = DateTime.Now;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Update settings from previous version
            if (Settings.Default.UpgradeRequired)
            {
                Settings.Default.Upgrade();
                Settings.Default.UpgradeRequired = false;
                Settings.Default.Save();
            }

            Thread.CurrentThread.CurrentUICulture = CultureInfo.CreateSpecificCulture(Settings.Default.SelectedCulture);

            // code to ensure that only one copy of the software is running.
            Mutex mutex;
            string strLoc = Assembly.GetExecutingAssembly().Location;
            FileSystemInfo fileInfo = new FileInfo(strLoc);
            string sExeName = fileInfo.Name;
            string mutexName = "Global\\" + sExeName;
            try
            {
                mutex = Mutex.OpenExisting(mutexName);

                //since it hasn’t thrown an exception, then we already have one copy of the app open.
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyProductAttribute), false);
                String appTitle = ((AssemblyProductAttribute)attributes[0]).Product;

                MessageBox.Show(StringResources.ProgramInstanceAlreadyRunning, appTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
                Environment.Exit(0);
            }
            catch
            {
                //since we didn’t find a mutex with that name, create one
                mutex = new Mutex(true, mutexName);
            }

            // Check Data directory
            if (SettingsUtil.SetDefaultDataDirIfEmpty(Settings.Default))
            {
                Settings.Default.Save();
            }

            string dataDir = Settings.Default.DataDirectory + Path.DirectorySeparatorChar;

            SongManager songManager = new SongManager(SettingsUtil.GetSongDirPath(Settings.Default));

            string imageDirPath = dataDir + Settings.Default.ImageDir;
            string thumbDirPath = dataDir + Settings.Default.ThumbDir;
            ImageManager imgManager = new ImageManager(imageDirPath, thumbDirPath)
            {
                DefaultThumbSize = Settings.Default.ThumbSize,
                DefaultEmptyColor = Settings.Default.ProjectionBackColor
            };

            string bibleDir = dataDir + "Bibles";
            BibleManager bibleManager = new BibleManager(bibleDir);

            if (Settings.Default.ShowLoadingScreen)
            {
                LoadingScreen ldg = new LoadingScreen(songManager, imgManager);
                ldg.SetLabel("PraiseBase Presenter wird gestartet...");
                ldg.Show();

                ldg.SetLabel("Prüfe Miniaturbilder...");
                imgManager.CheckThumbs();

                ldg.SetLabel("Lade Liederdatenbank...");
                songManager.Reload();

                GC.Collect();
                ldg.Close();
                ldg.Dispose();
            }
            else
            {
                imgManager.CheckThumbs();
                songManager.Reload();
                GC.Collect();
            }

            Console.WriteLine(@"Loading took " + (DateTime.Now - startTime).TotalSeconds + @" seconds!");

            string setlistFile = null;
            string songFile = null;

            // Detect if program is called with a setlist file as argument
            if (args.Length == 1)
            {
                if (File.Exists((args[0])))
                {
                    string ext = Path.GetExtension(args[0]);
                    if (ext == "." + SetlistWriter.FileExtension)
                    {
                        setlistFile = args[0];
                    }
                    else
                    {
                        songFile = args[0];
                    }
                }
            }

            Form mw;
            if (songFile != null)
            {
                mw = new SongEditor(Settings.Default, imgManager, songFile);
            }
            else
            {
                mw = new MainWindow(songManager, imgManager, bibleManager, setlistFile);
            }
            Application.Run(mw);
            GC.KeepAlive(mutex);
        }
    }
}