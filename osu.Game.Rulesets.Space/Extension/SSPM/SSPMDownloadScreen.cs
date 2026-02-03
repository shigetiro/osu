#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.IO.Network;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Framework.Screens;
using osu.Game.Beatmaps;
using osu.Game.Database;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Overlays;
using osu.Game.Overlays.Notifications;
using osu.Game.Rulesets;
using osu.Game.Screens;
using osuTK;
using osuTK.Graphics;
using Realms;

namespace osu.Game.Rulesets.Space.Extension.SSPM
{
    public partial class SSPMDownloadScreen : OsuScreen
    {
        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        [Resolved]
        private BeatmapManager beatmapManager { get; set; } = null!;

        [Resolved]
        private RulesetStore rulesets { get; set; } = null!;

        [Resolved]
        private INotificationOverlay? notifications { get; set; }

        [Resolved]
        private OsuColour colours { get; set; } = null!;

        [Resolved]
        private Storage storage { get; set; } = null!;

        private FillFlowContainer<RhythiaBeatmapPanel> mapsFlow = null!;
        private TextBox searchTextBox = null!;
        private LoadingLayer loadingLayer = null!;

        private OsuSpriteText pageText = null!;
        private GrayButton prevButton = null!;
        private GrayButton nextButton = null!;

        private int currentPage = 1;
        private int totalPages = 1;

        private CancellationTokenSource? searchCancellation;

        [BackgroundDependencyLoader]
        private void load()
        {
            InternalChildren = new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = colours.GreySeaFoamDark
                },
                new GridContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    RowDimensions = new[]
                    {
                        new Dimension(GridSizeMode.Absolute, 80),
                        new Dimension(),
                        new Dimension(GridSizeMode.Absolute, 50),
                    },
                    Content = new[]
                    {
                        new Drawable[]
                        {
                            new Container
                            {
                                RelativeSizeAxes = Axes.Both,
                                Padding = new MarginPadding(20),
                                Children = new Drawable[]
                                {
                                    new FillFlowContainer
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        AutoSizeAxes = Axes.Y,
                                        Direction = FillDirection.Horizontal,
                                        Spacing = new Vector2(10),
                                        Children = new Drawable[]
                                        {
                                            new OsuSpriteText
                                            {
                                                Text = "Search Rhythia Maps:",
                                                Anchor = Anchor.CentreLeft,
                                                Origin = Anchor.CentreLeft,
                                                Font = OsuFont.GetFont(size: 20, weight: FontWeight.Bold)
                                            },
                                            searchTextBox = new BasicTextBox
                                            {
                                                Width = 300,
                                                Height = 40,
                                                Anchor = Anchor.CentreLeft,
                                                Origin = Anchor.CentreLeft,
                                                CommitOnFocusLost = true,
                                            },
                                            new GrayButton(FontAwesome.Solid.Search)
                                            {
                                                Width = 100,
                                                Height = 40,
                                                Action = () => performSearch(searchTextBox.Text, 1),
                                                Anchor = Anchor.CentreLeft,
                                                Origin = Anchor.CentreLeft,
                                            }
                                        }
                                    }
                                }
                            }
                        },
                        new Drawable[]
                        {
                            new BasicScrollContainer
                            {
                                RelativeSizeAxes = Axes.Both,
                                Child = mapsFlow = new FillFlowContainer<RhythiaBeatmapPanel>
                                {
                                    RelativeSizeAxes = Axes.X,
                                    AutoSizeAxes = Axes.Y,
                                    Direction = FillDirection.Vertical,
                                    Spacing = new Vector2(10),
                                    Padding = new MarginPadding(20),
                                }
                            }
                        },
                        new Drawable[]
                        {
                            new Container
                            {
                                RelativeSizeAxes = Axes.Both,
                                Padding = new MarginPadding { Horizontal = 20 },
                                Children = new Drawable[]
                                {
                                    new FillFlowContainer
                                    {
                                        AutoSizeAxes = Axes.Both,
                                        Anchor = Anchor.Centre,
                                        Origin = Anchor.Centre,
                                        Direction = FillDirection.Horizontal,
                                        Spacing = new Vector2(10),
                                        Children = new Drawable[]
                                        {
                                            prevButton = new GrayButton(FontAwesome.Solid.ArrowLeft)
                                            {
                                                Width = 50,
                                                Height = 40,
                                                Action = () => changePage(-1),
                                                Enabled = { Value = false }
                                            },
                                            pageText = new OsuSpriteText
                                            {
                                                Text = "Page 1",
                                                Anchor = Anchor.CentreLeft,
                                                Origin = Anchor.CentreLeft,
                                                Font = OsuFont.GetFont(size: 20)
                                            },
                                            nextButton = new GrayButton(FontAwesome.Solid.ArrowRight)
                                            {
                                                Width = 50,
                                                Height = 40,
                                                Action = () => changePage(1),
                                                Enabled = { Value = false }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
                loadingLayer = new LoadingLayer(true)
            };

            searchTextBox.OnCommit += (sender, isNew) => performSearch(sender.Text, 1);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            performSearch("", 1);
        }

        private void changePage(int direction)
        {
            int newPage = currentPage + direction;
            if (newPage >= 1 && newPage <= totalPages)
            {
                performSearch(searchTextBox.Text, newPage);
            }
        }

        private void performSearch(string query, int page)
        {
            searchCancellation?.Cancel();
            searchCancellation = new CancellationTokenSource();

            loadingLayer.Show();
            mapsFlow.Clear();
            currentPage = page;

            Task.Run(async () =>
            {
                try
                {
                    // Construct URL: api/v2/rhythia/search?query=...&page=...
                    string url = $"{api.Endpoints.APIUrl}/api/v2/rhythia/search?query={Uri.EscapeDataString(query ?? "")}&page={page}";

                    var req = new WebRequest(url);
                    await req.PerformAsync(searchCancellation.Token);

                    var json = JObject.Parse(req.GetResponseString());
                    var docs = json["docs"];
                    int total = json["total"]?.Value<int>() ?? 0;
                    int limit = json["limit"]?.Value<int>() ?? 10;

                    totalPages = (int)Math.Ceiling((double)total / limit);
                    if (totalPages < 1) totalPages = 1;

                    Schedule(() =>
                    {
                        if (docs != null)
                        {
                            foreach (var doc in docs)
                            {
                                mapsFlow.Add(new RhythiaBeatmapPanel(doc, downloadMap));
                            }
                        }

                        pageText.Text = $"Page {currentPage} of {totalPages}";
                        prevButton.Enabled.Value = currentPage > 1;
                        nextButton.Enabled.Value = currentPage < totalPages;

                        loadingLayer.Hide();
                    });
                }
                catch (Exception ex)
                {
                    if (ex is OperationCanceledException) return;

                    Logger.Error(ex, "Failed to search Rhythia maps");
                    Schedule(() =>
                    {
                        notifications?.Post(new SimpleNotification
                        {
                            Text = "Failed to fetch maps from server.",
                            Icon = FontAwesome.Solid.ExclamationTriangle,
                        });
                        loadingLayer.Hide();
                    });
                }
            }, searchCancellation.Token);
        }

        private void downloadMap(int id, string title)
        {
            var notification = new ProgressNotification
            {
                Text = $"Downloading {title}...",
                CompletionText = $"Imported {title}!",
                State = ProgressNotificationState.Active,
            };
            notifications?.Post(notification);

            Task.Run(async () =>
            {
                try
                {
                    string url = $"{api.Endpoints.APIUrl}/api/v2/rhythia/download/{id}";
                    var req = new WebRequest(url);
                    await req.PerformAsync();

                    var data = req.GetResponseData();

                    string tempDir = storage.GetFullPath("temp_sspm");
                    Directory.CreateDirectory(tempDir);
                    string tempFile = Path.Combine(tempDir, $"{id}.sspm");

                    await File.WriteAllBytesAsync(tempFile, data);

                    notification.Text = "Converting and importing...";

                    var converter = new SSPMConverter(beatmapManager, rulesets, api);
                    string oszPath = converter.ConvertSSPMToOSZ(tempFile);

                    if (File.Exists(oszPath))
                    {
                        var oszBytes = await File.ReadAllBytesAsync(oszPath);
                        var importTask = new ImportTask(oszPath);
                        var importedSet = await beatmapManager.Import(importTask);
                        if (importedSet != null)
                        {
                            importedSet.PerformWrite(s =>
                            {
                                Realm realmContext = s.Realm!;
                                var managedSpaceRuleset =
                                    realmContext.Find<RulesetInfo>("osuspaceruleset")
                                    ?? realmContext.All<RulesetInfo>().FirstOrDefault(rs => rs.ShortName == "osuspaceruleset");

                                if (managedSpaceRuleset != null)
                                {
                                    foreach (var b in s.Beatmaps)
                                    {
                                        b.Ruleset = managedSpaceRuleset;
                                        b.OnlineID = id;
                                        string tags = b.Metadata.Tags ?? string.Empty;
                                        if (!tags.Contains("sspm", StringComparison.OrdinalIgnoreCase) ||
                                            !System.Text.RegularExpressions.Regex.IsMatch(tags, @"sspm\s+\d+", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                                        {
                                            b.Metadata.Tags = $"sspm {id} osuspaceruleset";
                                        }
                                    }
                                }
                            });
                        }

                        var uploadReq = new UploadSSPMMapRequest(oszBytes, Path.GetFileName(oszPath));
                        uploadReq.Success += () =>
                        {
                            notifications?.Post(new SimpleNotification
                            {
                                Text = "SSPM map registered",
                                Icon = FontAwesome.Solid.CheckCircle,
                            });
                        };
                        uploadReq.Failure += _ =>
                        {
                            var registerReq = new RegisterRhythiaMapRequest(id);
                            registerReq.Success += () =>
                            {
                                notifications?.Post(new SimpleNotification
                                {
                                    Text = "SSPM map registered",
                                    Icon = FontAwesome.Solid.CheckCircle,
                                });
                            };
                            registerReq.Failure += __ =>
                            {
                                notifications?.Post(new SimpleNotification
                                {
                                    Text = "SSPM map registration failed",
                                    Icon = FontAwesome.Solid.ExclamationTriangle,
                                });
                            };
                            if (api.State.Value == APIState.Offline)
                                api.Perform(registerReq);
                            else
                                api.Queue(registerReq);
                        };
                        if (api.State.Value == APIState.Offline)
                            await api.PerformAsync(uploadReq);
                        else
                            api.Queue(uploadReq);

                        // Clean up
                        try { File.Delete(tempFile); } catch { }
                        try { File.Delete(oszPath); } catch { }

                        notification.State = ProgressNotificationState.Completed;
                    }
                    else
                    {
                        throw new InvalidOperationException("Conversion failed (osz not found).");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Failed to download/import map");
                    notification.State = ProgressNotificationState.Cancelled;
                    notification.Text = "Import failed.";
                }
            });
        }

        private partial class RhythiaBeatmapPanel : CompositeDrawable
        {
            private readonly JToken data;
            private readonly Action<int, string> downloadAction;

            public RhythiaBeatmapPanel(JToken data, Action<int, string> downloadAction)
            {
                this.data = data;
                this.downloadAction = downloadAction;
            }

            [BackgroundDependencyLoader]
            private void load(OsuColour colours)
            {
                RelativeSizeAxes = Axes.X;
                Height = 80;
                CornerRadius = 10;
                Masking = true;

                int id = data["id"]?.Value<int>() ?? 0;
                string title = data["beatmap"]?["title"]?.Value<string>() ?? "Unknown Title";
                string artist = data["beatmap"]?["artist"]?.Value<string>() ?? "Unknown Artist";
                string mapper = data["beatmap"]?["ownerUsername"]?.Value<string>() ?? "Unknown Mapper";
                float stars = data["beatmap"]?["starRating"]?.Value<float>() ?? 0;
                string? coverUrl = data["cover"]?.Value<string>() ?? data["beatmap"]?["cover"]?.Value<string>() ?? data["beatmapset"]?["covers"]?["cover"]?.Value<string>();

                if (!string.IsNullOrEmpty(coverUrl))
                {
                    Logger.Log($"Found cover URL for map {id}: {coverUrl}");
                }

                InternalChildren = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = colours.GreySeaFoam,
                    },
                    !string.IsNullOrEmpty(coverUrl)
                        ? new OnlineCoverSprite(coverUrl)
                        {
                            RelativeSizeAxes = Axes.Both,
                            FillMode = FillMode.Fill,
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Alpha = 0.3f
                        }
                        : Empty(),
                    new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Padding = new MarginPadding(10),
                        Children = new Drawable[]
                        {
                            new FillFlowContainer
                            {
                                AutoSizeAxes = Axes.Both,
                                Direction = FillDirection.Vertical,
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                Children = new Drawable[]
                                {
                                    new OsuSpriteText
                                    {
                                        Text = $"{artist} - {title}",
                                        Font = OsuFont.GetFont(size: 20, weight: FontWeight.Bold)
                                    },
                                    new OsuSpriteText
                                    {
                                        Text = $"Mapped by {mapper} | {stars:F2} Stars",
                                        Font = OsuFont.GetFont(size: 14)
                                    }
                                }
                            },
                            new GrayButton(FontAwesome.Solid.Download)
                            {
                                Width = 100,
                                Height = 40,
                                Action = () => downloadAction(id, title),
                                Anchor = Anchor.CentreRight,
                                Origin = Anchor.CentreRight,
                            }
                        }
                    }
                };
            }
        }

        private partial class OnlineCoverSprite : Sprite
        {
            private readonly string url;

            [Resolved]
            private IRenderer renderer { get; set; } = null!;

            public OnlineCoverSprite(string url)
            {
                this.url = url;
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                Texture = renderer.WhitePixel; // Placeholder
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();
                loadCover();
            }

            private void loadCover()
            {
                if (string.IsNullOrEmpty(url)) return;

                Task.Run(async () =>
                {
                    try
                    {
                        var req = new WebRequest(url);
                        await req.PerformAsync();
                        var data = req.GetResponseData();
                        if (data != null)
                        {
                            // Ensure we are on the update thread when creating the texture
                            Schedule(() =>
                            {
                                try
                                {
                                    var texture = Texture.FromStream(renderer, new MemoryStream(data));
                                    Texture = texture;
                                }
                                catch (Exception ex)
                                {
                                    Logger.Error(ex, $"Failed to create texture from {url}");
                                }
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, $"Failed to fetch cover from {url}");
                    }
                });
            }
        }
    }
}
