﻿using Discord;
using NadekoBot.Common;
using NadekoBot.Common.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Core.Services;
using System;
using System.Threading.Tasks;
using Wof = NadekoBot.Modules.Gambling.Common.WheelOfFortune.WheelOfFortuneGame;
using NadekoBot.Modules.Gambling.Services;
using NadekoBot.Core.Modules.Gambling.Common;
using NadekoBot.Core.Common;
using System.Collections.Immutable;
using Image = SixLabors.ImageSharp.Image;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp;
using SixLabors.Primitives;

namespace NadekoBot.Modules.Gambling
{
    public partial class Gambling
    {
        public class WheelOfFortuneCommands : GamblingSubmodule<GamblingService>
        {
            private static readonly ImmutableArray<Tuple<decimal, string>> _results = new Tuple<decimal, string>[] {
            Tuple.Create(0.2m, "wheel_total_loss"),
            Tuple.Create(1.1m, "wheel_break_even"),
            Tuple.Create(2m, "wheel_mild_win"),
            Tuple.Create(0.01m, "wheel_total_loss"),
            Tuple.Create(-1m, "wheel_critfail"),
            Tuple.Create(0.01m, "wheel_total_loss"),
            Tuple.Create(1m, "wheel_break_even"),
            Tuple.Create(0.05m, "wheel_total_loss"),
            Tuple.Create(0.5m, "wheel_mild_loss"),
            Tuple.Create(0.6m, "wheel_mild_loss"),
            Tuple.Create(0.1m, "wheel_total_loss"),
            Tuple.Create(0.3m, "wheel_total_loss"),
            Tuple.Create(10m, "wheel_great_win"),
            Tuple.Create(0.4m, "wheel_mild_loss"),
            Tuple.Create(0.01m, "wheel_total_loss"),
            Tuple.Create(0.7m, "wheel_mild_loss")}.ToImmutableArray();

            private readonly decimal _mayhemMultiplier = 10m;
            private readonly ICurrencyService _cs;
            private readonly DbService _db;
            private readonly IImageCache _images;

            public WheelOfFortuneCommands(ICurrencyService cs, DbService db, IDataCache data)
            {
                _cs = cs;
                _db = db;
                _images = data.LocalImages;
            }

            [NadekoCommand, Usage, Description, Aliases]
            public async Task WheelOfFortune(ShmartNumber amount)
            {
                if (!await CheckBetMandatory(amount).ConfigureAwait(false))
                    return;

                if (!await _cs.RemoveAsync(Context.User.Id, "Wheel Of Fortune - bet", amount, gamble: true).ConfigureAwait(false))
                {
                    await ReplyErrorLocalized("not_enough", Bc.BotConfig.CurrencySign).ConfigureAwait(false);
                    return;
                }

                var _rng = new NadekoRandom();
                var result = _rng.Next(0, _results.Length);
                var wonAmountTemp = _results[result].Item1 * amount;
                string commentary = GetText(_results[result].Item2);
                var rotation = (result * 360)/_results.Length;
                var mayhem = _rng.Next(0,100);

                if(mayhem == 0) {
                    wonAmountTemp = wonAmountTemp * _mayhemMultiplier;
                    
                    await Context.Channel.SendConfirmAsync("Let's spin the "
                                                           +Format.Bold("Wheel of Mayhem")
                                                           +" for x"
                                                                       +_mayhemMultiplier
                                                                       +" multiplier, "
                                                                       +Context.User.Mention
                                                                       +"!")
                                         .ConfigureAwait(false);
                    System.Threading.Thread.Sleep(2000);
                    await Context.Channel.SendConfirmAsync("Ratatata-ta-ta-ta ta ta...!").ConfigureAwait(false);
                    System.Threading.Thread.Sleep(2000);
                    await Context.Channel.SendConfirmAsync("Ta...!").ConfigureAwait(false);
                    System.Threading.Thread.Sleep(2000);
                    await Context.Channel.SendConfirmAsync("...").ConfigureAwait(false);
                    System.Threading.Thread.Sleep(2000);
                    await Context.Channel.SendConfirmAsync("Ta!").ConfigureAwait(false);
                    System.Threading.Thread.Sleep(200);
                }
                
                long wonAmount = (long) wonAmountTemp; //remove the decimal part
                
                if(wonAmount > 0) {
                    await _cs.AddAsync(Context.User.Id, "Wheel Of Fortune - won", wonAmount, gamble: true)
                             .ConfigureAwait(false);
                } 
                else if (wonAmount < 0) {
                    await _cs.RemoveAsync(Context.User.Id, "Wheel Of Fortune - lost", -wonAmount, gamble: true)
                             .ConfigureAwait(false);
                }
                
                using (var bgImage = Image.Load(_images.Roulette))
                {   
                    var originalWidth = bgImage.Width;
                    var originalHeight = bgImage.Height;
                    
                    bgImage.Mutate(x => x.Rotate(-rotation));
                    
                    var widthCrop = (bgImage.Width - originalWidth)/2;
                    var heightCrop = (bgImage.Height - originalHeight)/2;
                        
                    bgImage.Mutate(x => x.Crop(new Rectangle(widthCrop, 
                                                             heightCrop, 
                                                             originalWidth, 
                                                             originalHeight)));
                    
                    using (var ptImage = Image.Load(_images.RoulettePointer)) {
                        var resizeOptions = new ResizeOptions();
                        resizeOptions.Mode = ResizeMode.Pad;
                        resizeOptions.Position = AnchorPositionMode.TopLeft;
                        resizeOptions.Size = new Size(originalWidth + (ptImage.Width / 2), originalHeight);
                        bgImage.Mutate(x => x.Resize(resizeOptions));
                        
                        var pointerPosX = bgImage.Width - ptImage.Width;
                        var pointerPosY = (bgImage.Height / 2) - (ptImage.Height / 2);
                        bgImage.Mutate(x => x.DrawImage(GraphicsOptions.Default, ptImage, new Point(pointerPosX, pointerPosY)));
                    }
                                               
                    using (var imgStream = bgImage.ToStream())
                    {
                        string outText;
                        long outAmount;
                        if(wonAmount >= 0) {
                            outText = GetText("won");
                            outAmount = wonAmount;
                        } else {
                            outText = GetText("lost");
                            outAmount = -wonAmount;
                        }
                        await Context.Channel.SendFileAsync(imgStream, 
                                                            "result.png", 
                                                            $@"{Format.Bold(Context.User.ToString())} {outText}: `{outAmount}`{Bc.BotConfig.CurrencySign}{System.Environment.NewLine}{commentary}")
                                             .ConfigureAwait(false);
                    }
                }
            }
        }
    }
}
