using Godot;
using Godot.Collections;

public partial class DeckViewWindow : Control
{
    [Export] public PackedScene? card_container_scene;

    private Array cached_card_containers = new Array(); // CardContainer
    private HFlowContainer? h_flow_container;

    public override void _Ready()
    {
        h_flow_container = GetNodeOrNull<HFlowContainer>("HFlowContainer");
    }

    public void ClearDisplay()
    {
        if (h_flow_container == null) return;
        foreach (object childObj in h_flow_container.GetChildren())
        {
            // GetChildren returns Variants boxed as object; cast to Node first
            var node = childObj as Node;
            var cc = node as CardContainer;
            if (cc != null && cc.PlayableCard != null)
                cc.RemoveChild(cc.PlayableCard);
            if (node != null)
                h_flow_container.RemoveChild(node);
        }
    }

    public void DisplayCardList(Array cardsWithID)
    {
        ClearDisplay();

        while (cached_card_containers.Count < cardsWithID.Count)
        {
            if (card_container_scene == null) break;
            var inst = card_container_scene.Instantiate();
            // Instantiate can return a Node boxed as object/Variant; ensure we get Node first
            var node = inst as Node;
            var cc = node as CardContainer;
            if (cc != null) cached_card_containers.Add(cc);
        }

        for (int i = 0; i < cardsWithID.Count; i++)
        {
            object elem = cardsWithID[i];
            var cardWithID = elem as CardWithID;
            object cachedObj = cached_card_containers[i];
            var card_container = cachedObj as CardContainer;
            if (card_container != null && cardWithID != null && h_flow_container != null)
            {
                h_flow_container.AddChild(card_container);
                card_container.card = cardWithID.Card;
            }
        }
    }
}

