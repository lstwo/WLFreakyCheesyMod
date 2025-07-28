using HawkNetworking;

namespace WLButSlenderman;

public class CollectibleNetwork : HawkNetworkBehaviour
{
    private byte RPC_COLLECT;
    private bool collected;

    protected override void RegisterRPCs(HawkNetworkObject networkObject)
    {
        base.RegisterRPCs(networkObject);

        networkObject.RegisterRPC(ClientCollect);
    }

    public void Collect()
    {
        networkObject.SendRPC(RPC_COLLECT, RPCRecievers.All);
    }

    private void ClientCollect(HawkNetReader reader, HawkRPCInfo info)
    {
        if (collected)
        {
            return;
        }
        
        collected = true;
        CollectibleManager.CollectedPfps++;
        VanishComponent.VanishAndDestroy(gameObject);
    }
}