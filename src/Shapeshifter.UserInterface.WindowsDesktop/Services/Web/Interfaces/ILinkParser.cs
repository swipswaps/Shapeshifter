﻿using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Dependencies.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces
{
    public interface ILinkParser : ISingleInstance
    {
        Task<IEnumerable<string>> ExtractLinksFromTextAsync(string text);

        Task<bool> HasLinkAsync(string text);

        Task<bool> HasLinkOfTypeAsync(string text, LinkType linkType);

        Task<bool> IsValidLinkAsync(string link);

        LinkType GetLinkType(string link);
    }
}
