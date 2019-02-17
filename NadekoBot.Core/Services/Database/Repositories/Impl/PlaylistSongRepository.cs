using NadekoBot.Core.Services.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace NadekoBot.Core.Services.Database.Repositories.Impl
{
    public class PlaylistSongRepository : Repository<PlaylistSong>, IPlaylistSongRepository
    {
        public PlaylistSongRepository(DbContext context) : base(context)
        {
        }
    }
}
