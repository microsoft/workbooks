//
// Author:
//   Larry Ewing <lewing@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using System;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;

using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Configuration;

using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

using Xamarin.Interactive.Core;
using Xamarin.Interactive.Workbook.Models;

namespace Xamarin.Interactive.Client.Web.Controllers
{
    public class Card {
        public string CardId { get; set; }
        public string Icon { get; set;  }
        public string ContentUrl { get; set; }
        public string ContentString { get; set; }
        public string Guid { get; set; }
        public string Title { get; set; }
        public string Name { get; set; }
        //public ImmutableArray<AgentType> PlatformTargets { get; set; }
        //public ImmutableArray<InteractivePackageDescription> Packages { get; set; }
    }

    [Route("/api/workbook")]
    public sealed class WorkbookController : Controller
    {
        readonly IConfiguration configuration;
        readonly IHostingEnvironment hosting;

        public WorkbookController (IConfiguration configuration, IHostingEnvironment hosting)
        {
            this.configuration = configuration;
            this.hosting = hosting;
        }

        public IActionResult Index()
        {
            var workbooks = configuration.GetSection("workbooks");
            var workbookDir = workbooks.GetValue<string> ("path");

            if (String.IsNullOrEmpty (workbookDir))
                return new JsonResult(new { });

            var fileProvider = hosting.WebRootFileProvider;
            var path = fileProvider.GetFileInfo (workbookDir);
            var dir = fileProvider.GetDirectoryContents (workbookDir);

            var cards = new Dictionary<string, Card>();
            foreach (var book in workbooks.GetSection ("books").GetChildren ()) {
                var card = new Card
                {
                    CardId = book.GetValue<string>(nameof(Card.CardId)),
                    ContentUrl = book.GetValue<string>(nameof(Card.ContentUrl)),
                    ContentString = book.GetValue<string>(nameof(Card.ContentString)),
                    Title = book.GetValue<string> (nameof (Card.Title)),
                    Name = book.GetValue<string> (nameof (Card.Name)),
                    Icon = book.GetValue<string> (nameof (Card.Icon))
                };
                if (!String.IsNullOrEmpty (card.Name))
                    cards[card.Name] = card;

                card.CardId = card.CardId ?? card.Name.Replace(".workbook", "");
            }

            var contents = dir.Where (book => !book.IsDirectory && book.PhysicalPath.EndsWith (".workbook")).Select(book =>
            {
                var reader = new StreamReader(book.CreateReadStream ());
                var document = new WorkbookDocument();
                document.Read(reader);

                var manifest = new WorkbookDocumentManifest();
                manifest.Read(document);

                return new Card
                {
                    CardId = book.Name.Replace(".workbook", ""),
                    ContentUrl = book.PhysicalPath.Replace (hosting.WebRootPath, ""),
                    Name = book.Name,
                    Title = manifest.Title,
                    Guid = manifest.Guid.ToString (),
                };
            });

            foreach (var card in contents) {
                if (cards.TryGetValue (card.Name, out var configCard)) {
                    configCard.Guid = card.Guid;
                    configCard.Title = card.Title ?? configCard.Title;
                    configCard.ContentUrl = card.ContentUrl;
                    configCard.CardId = configCard.CardId ?? configCard.Name.Replace(".workbook", "");
                } else {
                    cards[card.Name] = card;
                }
            }

            return new JsonResult(cards.Values);
        }
    }
}