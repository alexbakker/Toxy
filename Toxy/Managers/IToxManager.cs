using SharpTox.Av;
using SharpTox.Core;

namespace Toxy.Managers
{
    public interface IToxManager
    {
        void SwitchProfile(Tox tox, ToxAv toxAv);
    }
}
