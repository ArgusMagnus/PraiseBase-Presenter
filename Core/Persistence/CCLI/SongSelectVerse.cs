﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PraiseBase.Presenter.Persistence.CCLI
{
    public class SongSelectVerse
    {
        /// <summary>
        /// Caption
        /// </summary>
        public string Caption { get; set; }

        /// <summary>
        /// Lyrics
        /// </summary>
        public List<string> Lines { get; private set; }

        public SongSelectVerse()
        {
            Lines = new List<string>();
        }
    }
}