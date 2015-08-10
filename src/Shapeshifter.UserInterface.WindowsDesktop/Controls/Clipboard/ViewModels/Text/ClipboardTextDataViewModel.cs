﻿using Shapeshifter.Core.Data;
using Shapeshifter.Core.Data.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.ViewModels.Text.Interfaces;
using System;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows;

namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.ViewModels
{
    class ClipboardTextDataViewModel : ClipboardDataViewModel<IClipboardTextData>, IClipboardTextDataViewModel
    {
        private static readonly Regex whitespaceSubstitutionExpression;

        static ClipboardTextDataViewModel()
        {
            whitespaceSubstitutionExpression = new Regex(@"\s+", RegexOptions.Compiled);
        }

        public ClipboardTextDataViewModel()
        {
            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                Data = new DesignerClipboardTextDataFacade();
            }
        }

        public ClipboardTextDataViewModel(ClipboardTextData data) : base(data)
        {
        }

        public string FriendlyText
        {
            get
            {
                var text = Data.Text.Trim();
                text = whitespaceSubstitutionExpression.Replace(text, " ");
                text = text.Substring(0, Math.Min(text.Length, 512));

                return text;
            }
        }
    }
}
