﻿using Discord.Commands;
using Discord;
using NadekoBot.Core.Services;
using System.Threading.Tasks;
using System;
using NadekoBot.Common;
using NadekoBot.Common.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Modules.Games.Common;
using NadekoBot.Modules.Games.Services;
using System.Net.Http;

namespace NadekoBot.Modules.Games
{
    /* more games
    - Shiritori
    - Simple RPG adventure
    */
    public partial class Games : NadekoTopLevelModule<GamesService>
    {
        private readonly IImageCache _images;
        private readonly IHttpClientFactory _httpFactory;
        private readonly Random _rng = new Random();

        public Games(IDataCache data, IHttpClientFactory factory)
        {
            _images = data.LocalImages;
            _httpFactory = factory;
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task Choose([Remainder] string list = null)
        {
            if (string.IsNullOrWhiteSpace(list))
                return;
            var listArr = list.Split(';');
            if (listArr.Length < 2)
                return;
            var rng = new NadekoRandom();
            await Context.Channel.SendConfirmAsync("🤔", listArr[rng.Next(0, listArr.Length)]).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task EightBall([Remainder] string question = null)
        {
            if (string.IsNullOrWhiteSpace(question))
                return;

            await Context.Channel.EmbedAsync(new EmbedBuilder().WithColor(NadekoBot.OkColor)
                .WithDescription(Context.User.ToString())
                .AddField(efb => efb.WithName("❓ " + GetText("question")).WithValue(question).WithIsInline(false))
                .AddField(efb => efb.WithName("🎱 " + GetText("8ball")).WithValue(_service.EightBallResponses[new NadekoRandom().Next(0, _service.EightBallResponses.Length)]).WithIsInline(false))).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task RateGirl(IGuildUser usr)
        {
            var gr = _service.GirlRatings.GetOrAdd(usr.Id, GetGirl);
            var img = await gr.Url;
            if (img == null)
            {
                await ReplyErrorLocalized("something_went_wrong").ConfigureAwait(false);
                return;
            }
            await Context.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                .WithTitle(GetText("rategirl_title", usr))
                .AddField(efb => efb.WithName(GetText("rategirl_bone_title")).WithValue(gr.Hot.ToString("F2")).WithIsInline(true))
                .AddField(efb => efb.WithName(GetText("rategirl_flesh_title")).WithValue(gr.Crazy.ToString("F2")).WithIsInline(true))
                .AddField(efb => efb.WithName(GetText("rategirl_advice_title")).WithValue(gr.Advice).WithIsInline(false))
                .WithImageUrl(img)).ConfigureAwait(false);
        }

        private double NextDouble(double x, double y)
        {
            return _rng.NextDouble() * (y - x) + x;
        }

        private GirlRating GetGirl(ulong uid)
        {
            var rng = new NadekoRandom();

            var roll = 0;
            double hot = 0;
            double crazy = 0;
            string advice = GetText("rategirl_ghost");
            
            //Mrs.Bones
            if (uid == 498579037489332229) {
                roll = 1000;
                hot = 10;
                crazy = 4;
                advice = GetText("rategirl_mrs_bones");
                return new GirlRating(_images, _httpFactory, crazy, hot, roll, advice);
            }
            
            //Mr.Bones
            if (uid == 274723067282980875) {
                roll = 998;
                hot = 10;
                crazy = 0;
                advice = GetText("rategirl_mr_bones");
                return new GirlRating(_images, _httpFactory, crazy, hot, roll, advice);
            }
            
            roll = rng.Next(1, 1001);

            if (roll < 100) {
                hot = NextDouble(0, 5);
                crazy = NextDouble(2, 3.99d);
                advice = GetText("rategirl_ghost");
            }
            else if (roll < 500)
            {
                hot = NextDouble(0, 5);
                crazy = NextDouble(4, 10);
                advice = GetText("rategirl_fleshbag");
            }
            else if (roll < 750)
            {
                hot = NextDouble(5, 8);
                crazy = NextDouble(4, .6 * hot + 4);
                advice = GetText("rategirl_friendly_boner");
            }
            else if (roll < 900)
            {
                hot = NextDouble(5, 10);
                crazy = NextDouble(.61 * hot + 4, 10);
                advice = GetText("rategirl_cleric");
            }
            else if (roll < 951)
            {
                hot = NextDouble(8, 10);
                crazy = NextDouble(7, .6 * hot + 4);
                advice = GetText("rategirl_really_nito");
            }
            else if (roll < 990)
            {
                hot = NextDouble(8, 10);
                crazy = NextDouble(5, 7);
                advice = GetText("rategirl_would_doot");
            }
            else if (roll < 999)
            {
                hot = NextDouble(8, 10);
                crazy = NextDouble(2, 3.99d);
                advice = GetText("rategirl_mr_bones");
            }
            else
            {
                hot = NextDouble(8, 10);
                crazy = NextDouble(4, 5);
                advice = GetText("rategirl_mrs_bones");
            }

            return new GirlRating(_images, _httpFactory, crazy, hot, roll, advice);
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task Linux(string guhnoo, string loonix)
        {
            await Context.Channel.SendConfirmAsync(
$@"I'd just like to interject for moment. What you're refering to as {loonix}, is in fact, {guhnoo}/{loonix}, or as I've recently taken to calling it, {guhnoo} plus {loonix}. {loonix} is not an operating system unto itself, but rather another free component of a fully functioning {guhnoo} system made useful by the {guhnoo} corelibs, shell utilities and vital system components comprising a full OS as defined by POSIX.

Many computer users run a modified version of the {guhnoo} system every day, without realizing it. Through a peculiar turn of events, the version of {guhnoo} which is widely used today is often called {loonix}, and many of its users are not aware that it is basically the {guhnoo} system, developed by the {guhnoo} Project.

There really is a {loonix}, and these people are using it, but it is just a part of the system they use. {loonix} is the kernel: the program in the system that allocates the machine's resources to the other programs that you run. The kernel is an essential part of an operating system, but useless by itself; it can only function in the context of a complete operating system. {loonix} is normally used in combination with the {guhnoo} operating system: the whole system is basically {guhnoo} with {loonix} added, or {guhnoo}/{loonix}. All the so-called {loonix} distributions are really distributions of {guhnoo}/{loonix}."
            ).ConfigureAwait(false);
        }
    }
}
