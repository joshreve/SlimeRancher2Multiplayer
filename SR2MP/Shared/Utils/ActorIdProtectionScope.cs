using System;
using Il2CppMonomiPark.SlimeRancher.DataModel;

namespace SR2MP.Shared.Utils;

internal struct ActorIdProtectionScope : IDisposable
{
    private readonly long _originalNextActorId;
    private readonly bool _isActive;

    public ActorIdProtectionScope()
    {
        var model = SR2MP.GlobalVariables.GameState;
        if (model != null && model._actorIdProvider != null)
        {
            _originalNextActorId = model._actorIdProvider._nextActorId;
            model._actorIdProvider._nextActorId = 3000000000L;
            _isActive = true;
        }
        else
        {
            _originalNextActorId = 0L;
            _isActive = false;
        }
    }

    public void Dispose()
    {
        if (_isActive)
        {
            var model = SR2MP.GlobalVariables.GameState;
            if (model != null && model._actorIdProvider != null)
            {
                model._actorIdProvider._nextActorId = _originalNextActorId;
            }
        }
    }
}
