using Discord;
using NadekoBot.Common.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Core.Services;
using System.Tuple;
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
            Tuple.Create(0.1m, "wheel_total_loss"),
            Tuple.Create(0.5m, "wheel_mild_loss"),
            Tuple.Create(0.2m, "wheel_total_loss"),
            Tuple.Create(10m, "wheel_great_win"),
            Tuple.Create(-1m, "wheel_critfail"),
            Tuple.Create(0.01m, "wheel_total_loss"),
            Tuple.Create(0.9m, "wheel_break_even"),
            Tuple.Create(0.3m, "wheel_total_loss"),
            Tuple.Create(0.7m, "wheel_mild_loss"),
            Tuple.Create(1.5m, "wheel_mild_win"),
            Tuple.Create(1.8m, "wheel_mild_win"),
            Tuple.Create(1m, "wheel_break_even"),
            Tuple.Create(1.8m, "wheel_mild_win"),
            Tuple.Create(2m, "wheel_mild_win"),
            Tuple.Create(1.1m, "wheel_break_even"),
            Tuple.Create(1.2m, "wheel_break_even")}.ToImmutableArray();

            private readonly decimal _mayhamMultiplier = 10m;
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
                var amount = _results[result].Item1 * amount;
                string commentary = GetText(_results[result].Item2);
                var rotation = (result * 360)/_results.Length;
                var mayham = _rng.Next(0,100);

                if(mayham == 0) {
                    await Context.Channel.SendConfirmAsync(Format.Bold("Let's spin the Wheel of Mayham for x"+_mayhamMultiplier+" multiplier, "+context.User.Mention+"!")).ConfigureAwait(false);
                    System.Threading.Thread.Sleep(500);
                    await Context.Channel.SendConfirmAsync(Format.Bold("Ratatata-ta-ta-ta ta ta...!")).ConfigureAwait(false);
                    System.Threading.Thread.Sleep(500);
                    await Context.Channel.SendConfirmAsync(Format.Bold("Ta...!")).ConfigureAwait(false);
                    System.Threading.Thread.Sleep(1000);
                    await Context.Channel.SendConfirmAsync(Format.Bold("...")).ConfigureAwait(false);
                    System.Threading.Thread.Sleep(2000);
                    await Context.Channel.SendConfirmAsync(Format.Bold("Ta!")).ConfigureAwait(false);
                    System.Threading.Thread.Sleep(200);
                    amount = amount * _mayhamMultiplier;
                }
                
                if(amount > 0) {
                    await _cs.AddAsync(_userId, "Wheel Of Fortune - won", amount, gamble: true)
                             .ConfigureAwait(false);
                } 
                else if (amount < 0) {
                    await _cs.RemoveAsync(_userId, "Wheel Of Fortune - lost", -amount, gamble: true)
                             .ConfigureAwait(false);
                }
                
                using (var bgImage = Image.Load(_images.Roulette))
                {   
                    var originalWidth = bgImage.Width;
                    var originalHeight = bgImage.Height;
                    
                    bgImage.Mutate(x => x.Rotate(rotation));
                    
                    var widthCrop = (bgImage.Width - originalWidth)/2;
                    var heightCrop = (bgImage.Height - originalHeight)/2;
                        
                    bgImage.Mutate(x => x.Crop(new Rectangle(widthCrop, heightCrop, bgImage.Width - widthCrop, bgImage.Height - heightCrop)));
                    
                    using (var ptImage = Image.Load(_images.RoulettePointer)) {
                        var pointerPosX = bgImage.Width - ptImage.Width;
                        var pointerPosY = (bgImage.Height / 2) - (ptImage.Height / 2);
                        bgImage.Mutate(x => x.DrawImage(GraphicsOptions.Default, ptImage, new Point(pointerPosX, pointerPosY)));
                    }
                                               
                    using (var imgStream = bgImage.ToStream())
                    {
                        string wonText;
                        var wonAmount;
                        if(amount >= 0) {
                            wonText = GetText("won");
                            wonAmount = amount;
                        } else {
                            wonText = GetText("lost");
                            wonAmount = -amount;
                        }
                        await Context.Channel.SendFileAsync(imgStream, 
                                                            "result.png", 
                                                            $@"{Context.User.ToString()} {wonText}: {wonAmount + Bc.BotConfig.CurrencySign}\n{commentary}")
                                             .ConfigureAwait(false);
                    }
                }
            }
        }
    }
}
