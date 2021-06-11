using System.Collections.Generic;
using System.Threading.Tasks;
using Milki.Extensions.Audio.Subchannels;

namespace Milki.Extensions.Audio
{
    public interface ISoundElementsProvider
    {
        Task<IEnumerable<SoundElement>> GetSoundElements();
    }
}