﻿namespace Octgn.Core.DataExtensionMethods
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Data;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Xml.Linq;

    using Octgn.Core.DataManagers;
    using Octgn.DataNew;
    using Octgn.DataNew.Entities;
    using Octgn.DataNew.FileDB;
    using Octgn.Library;
    using Octgn.Library.Exceptions;

    public static class GameExtensionMethods
    {
        internal static IFileSystem IO {
            get
            {
                return io ?? (io = new FileSystem());
            }
            set
            {
                io = value;
            }
        }
        private static IFileSystem io;

        public static IEnumerable<Set> Sets(this Game game)
        {
            return SetManager.Get().Sets.Where(x => x.GameId == game.Id);
        }

        public static Game Install(this Game game)
        {
            DbContext.Get().Save(game);
            return game;
        }

        public static Game UpdateGameHash(this Game game, string hash)
        {
            game = GameManager.Get().GetById(game.Id);
            game.FileHash = hash;
            DbContext.Get().Save(game);
            return game;
        }

        public static string GetFullPath(this Game game)
        {
            var ret = "";
            ret = IO.Path.Combine(Paths.DataDirectory, "Games");
            ret = IO.Path.Combine(ret, game.Id.ToString());
            ret = IO.Path.Combine(ret, "Defs");
            ret = IO.Path.Combine(ret, game.Filename);
            return ret;
        }

        public static Deck CreateDeck(this Game game)
        {
            var deck = new Deck { GameId = game.Id };
            deck.Sections = game.DeckSections.Select(x=> new Section{Name=x,Cards = new List<IMultiCard>()}).ToList();
            return deck;
        }

        public static IEnumerable<GameScript> GetScripts(this Game game)
        {
            return DbContext.Get().Scripts.Where(x => x.GameId == game.Id);
        }

        public static string GetInstallPath(this Game game)
        {
            return IO.Path.Combine(IO.Path.Combine(Paths.DataDirectory, "Games"), game.Id.ToString());
        }

        public static Uri GetCardBackUri(this Game game)
        {
            var path = IO.Path.Combine(game.GetInstallPath(), game.CardBack);
            var ret = new Uri(path);
            return ret;
        }

        public static string GetDefaultDeckPath(this Game game)
        {
            return IO.Path.Combine(Paths.DataDirectory, "Decks");
        }

        public static Card GetCardByName(this Game game, string name)
        {
            var g = GameManager.Get().GetById(game.Id);
            if (g == null) return null;
            return g.Sets().SelectMany(x=> x.Cards).FirstOrDefault(y =>y.Name == name);
        }

        public static Card GetCardById(this Game game, Guid id)
        {
            var g = GameManager.Get().GetById(game.Id);
            if (g == null) return null;
            return g.Sets().SelectMany(x => x.Cards).FirstOrDefault(y => y.Id == id);
        }

        public static Set GetSetById(this Game game, Guid id)
        {
            var g = GameManager.Get().GetById(game.Id);
            if (g == null) return null;
            return g.Sets().FirstOrDefault(x => x.Id == id);
        }

        public static IEnumerable<Marker> GetAllMarkers( this Game game)
        {
            var g = GameManager.Get().GetById(game.Id);
            if (g == null) return new List<Marker>();
            return g.Sets().SelectMany(x => x.Markers);
        }

        public static Pack GetPackById(this Game game, Guid id)
        {
            var g = GameManager.Get().GetById(game.Id);
            if (g == null) return null;
            return g.Sets().SelectMany(x => x.Packs).FirstOrDefault(x => x.Id == id);
        }

        public static IEnumerable<PropertyDef> AllProperties(this Game game)
        {
            var g = GameManager.Get().GetById(game.Id);
            if (g == null) return new List<PropertyDef>();
            return Enumerable.Repeat(new PropertyDef{Name="Name", Type = PropertyType.String}, 1).Union(game.CustomProperties);
        }

        public static IEnumerable<Card> AllCards(this Game game)
        {
            var g = GameManager.Get().GetById(game.Id);
            if (g == null) return new List<Card>();
            return g.Sets().SelectMany(x => x.Cards);
        }

        public static DataTable ToDataTable(this IEnumerable<Card> cards, Game game)
        {
            DataTable table = new DataTable();
            
            var values = new object[game.CustomProperties.Count + 5];
            var defaultValues = new object[game.CustomProperties.Count + 5];
            var indexes = new Dictionary<int, string>();
            var setCache = new Dictionary<Guid, string>();
            var i = 0 + 5;
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("SetName", typeof(string));
            table.Columns.Add("set_id", typeof(String));
            table.Columns.Add("img_uri", typeof(String));
            table.Columns.Add("id", typeof(string));
            defaultValues[0] = "";
            defaultValues[1] = "";
            defaultValues[2] = "";
            defaultValues[3] = "";
            defaultValues[4] = "";
            foreach (var prop in game.CustomProperties)
            {
                switch (prop.Type)
                {
                    case PropertyType.String:
                        table.Columns.Add(prop.Name, typeof(string));
                        defaultValues[i] = "";
                        break;
                    case PropertyType.Integer:
                        table.Columns.Add(prop.Name, typeof(double));
                        defaultValues[i] = 0;
                        break;
                    case PropertyType.GUID:
                        table.Columns.Add(prop.Name, typeof(Guid));
                        defaultValues[i] = Guid.Empty;
                        break;
                    case PropertyType.Char:
                        table.Columns.Add(prop.Name, typeof(char));
                        defaultValues[i] = 0;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                indexes.Add(i, prop.Name);
                i++;
            }

            foreach (Card item in cards)
            {
                for (i = 5; i < values.Length; i++)
                {
                    values[i] = defaultValues[i];
                }
                values[0] = item.Name;
                if(!setCache.ContainsKey(item.SetId))
                    setCache.Add(item.SetId,item.GetSet().Name);
                values[1] = setCache[item.SetId];
                values[2] = item.SetId;
                values[3] = item.ImageUri;
                values[4] = item.Id;
                foreach (var prop in item.Properties)
                {
                    values[indexes.First(x=>x.Value == prop.Key.Name).Key] = prop.Value;
                }
                   
                table.Rows.Add(values);
            }
            return table;   
        }

        public static Deck LoadDeck(this Game game, string filename)
        {
            if (game == null) throw new ArgumentNullException("game");

            XDocument doc;
            Guid gameId = new Guid();
            try
            {
                doc = XDocument.Load(filename);
            }
            catch (Exception e)
            {
                throw new FileNotReadableException(e);
            }

            if (doc.Root != null)
            {
                XAttribute gameAttribute = doc.Root.Attribute("game");
                if (gameAttribute == null)
                    throw new InvalidFileFormatException("The <deck> tag is missing the 'game' attribute");

                try
                {
                    gameId = new Guid(gameAttribute.Value);
                }
                catch
                {
                    throw new InvalidFileFormatException("The game attribute is not a valid GUID");
                }
            }

            if (gameId != game.Id) throw new WrongGameException(gameId, game.Id.ToString());

            Deck deck;
            try
            {
                var isShared = doc.Root.Attr<bool>("shared");
                IEnumerable<string> defSections = isShared ? game.SharedDeckSections : game.DeckSections;

                deck = new Deck { GameId = game.Id, IsShared = isShared };
                if (doc.Root != null)
                {
                    IEnumerable<Section> sections = from section in doc.Root.Elements("section")
                                                    let xAttribute = section.Attribute("name")
                                                    where xAttribute != null
                                                    select new Section()
                                                    {
                                                        Name = xAttribute.Value,
                                                        Cards = new ObservableCollection<IMultiCard>
                                                            (from card in section.Elements("card")
                                                             select new MultiCard
                                                             {
                                                                 Id = new Guid(card.Attr<string>("id")),
                                                                 Name = card.Value,
                                                                 Quantity =card.Attr<byte>("qty", 1)
                                                             })
                                                    };
                    var allSections = new Section[defSections.Count()];
                    int i = 0;
                    foreach (string sectionName in defSections)
                    {
                        allSections[i] = sections.FirstOrDefault(x => x.Name == sectionName);
                        if (allSections[i] == null) allSections[i] = new Section { Name = sectionName };
                        ++i;
                    }
                    deck.Sections = allSections;
                }
            }
            catch
            {
                throw new InvalidFileFormatException();
            }
            // Matches with actual cards in database

            foreach (var sec in deck.Sections)
            {
                var newList = (from e in sec.Cards let card = game.GetCardById(e.Id) select card.ToMultiCard(e.Quantity)).ToList();
                foreach(var n in newList)
                    sec.Cards.Add(n);
            }

            return deck;
        }

        public static void DeleteSet(this Game game, Set set)
        {
            SetManager.Get().UninstallSet(set);
        }
    }
}