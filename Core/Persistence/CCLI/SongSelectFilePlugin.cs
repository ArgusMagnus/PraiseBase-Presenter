﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PraiseBase.Presenter.Model.Song;

namespace PraiseBase.Presenter.Persistence.CCLI
{
    public class SongSelectFilePlugin : AbstractSongFilePlugin<SongSelectFile>
    {
        public SongSelectFilePlugin() : base()
        {
            reader = new SongSelectFileReader();
            mapper = new SongSelectFileMapper();
        }

        public override string GetFileExtension() { return ".usr"; }

        public override string GetFileTypeDescription() { return "SongSelect Import File"; }
    }
}