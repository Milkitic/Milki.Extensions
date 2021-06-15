using System.Collections.Generic;
using System.Threading.Tasks;

namespace Milki.Extensions.MixPlayer
{
    public interface ISoundElementsProvider
    {
        Task<IEnumerable<SoundElement>> GetSoundElements();
    }
}