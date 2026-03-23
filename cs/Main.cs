using Godot;
using Godot.Collections;
using System;

public partial class Main : Node2D
{
    [Export] public Character? player_character;
    [Export] public Character? enemy_character;

    [Export] public CardData? gaslight_card_data;
    [Export] public CardData? overcompensate_card_data;
    [Export] public CardData? amnesia_card_data;
    [Export] public CardData? denial_draw_card_data;
    [Export] public CardData? more_mana_card_data;
    [Export] public CardData? exhaust_test_card_data;
    [Export] public CardData? appeal_to_nature_card_data;
    [Export] public CardData? strawman_card_data;
    [Export] public CardData? ad_hominem_card_data;
    [Export] public CardData? bandwagon_card_data;
    [Export] public CardData? false_lead_card_data;

    [Export] public float turn_delay = 2.0f;
    [Export] public PackedScene playable_card_scene;

    public bool debug_mode = true;

    private Hand? hand;
    private ManaOrb? mana_orb;
    private GameController? game_controller;
    private Button? end_turn_button;
    private PlayableDeckUI? view_deck_button;
    private DeckViewWindow? deck_view_window;
    private DeckViewControl? deck_view_control;
    private PlayableDeckUI? draw_pile;
    private PlayableDeckUI? discard_pile;
    private Deck deck = new Deck();
    private ColorRect? game_over_color_rect;
    private ColorRect? fade_in_color_rect;
    private TextureButton? view_map_button;
    private Map? map;
    private TurnAnnouncer? turn_announcer;
    private Rewards? rewards;
    private SecrecyBar? secrecy_bar;
    private ChoiceRemoveCards? remove_choose_a_card;

    private int enemy_character_state = 0;
    private bool game_won = false;
    private int rewards_received = 0;
    private int ascension_level = 0;
    private float ascension_modifier = 1.1f;

    private float _original_music_volume = 0f;

    public override void _Ready()
    {
        hand = GetNodeOrNull<Hand>("CanvasLayer/Hand");
        if (hand != null)
        {
            GD.Print($"[MAIN DEBUG] hand resolved: path={hand.GetPath()} parent={hand.GetParent()?.Name} hand.GlobalPosition={hand.GlobalPosition}");
            // Enable hand debug logging so RepositionCards prints diagnostic info.
            try
            {
                hand.debug_log_spacing = true;
                GD.Print("[MAIN DEBUG] Enabled Hand.debug_log_spacing for diagnostics.");
            }
            catch { }
        }
        else
        {
            GD.PrintErr("[MAIN DEBUG] hand is null after GetNodeOrNull('CanvasLayer/Hand')");
        }
        mana_orb = GetNodeOrNull<ManaOrb>("ManaOrb");
        game_controller = GetNodeOrNull<GameController>("GameController");
        end_turn_button = GetNodeOrNull<Button>("EndTurnButton");
        // ViewDeckButton, DrawPile and DiscardPile are under the "Card Piles" node in the scene
        view_deck_button = GetNodeOrNull<PlayableDeckUI>("Card Piles/ViewDeckButton");
        deck_view_window = GetNodeOrNull<DeckViewWindow>("DeckViewWindow");
        deck_view_control = GetNodeOrNull<DeckViewControl>("DeckViewControl");
        draw_pile = GetNodeOrNull<PlayableDeckUI>("Card Piles/DrawPile");
        discard_pile = GetNodeOrNull<PlayableDeckUI>("Card Piles/DiscardPile");
        GD.Print($"[MAIN DEBUG] draw_pile resolved={(draw_pile != null)} discard_pile resolved={(discard_pile != null)}");
        game_over_color_rect = GetNodeOrNull<ColorRect>("GameOverColorRect");
        fade_in_color_rect = GetNodeOrNull<ColorRect>("CanvasLayer/FadeInColorRect");
        view_map_button = GetNodeOrNull<TextureButton>("ViewMapButton");
        map = GetNodeOrNull<Map>("Map");
        turn_announcer = GetNodeOrNull<TurnAnnouncer>("TurnAnnouncer");
        rewards = GetNodeOrNull<Rewards>("Rewards");
        secrecy_bar = GetNodeOrNull<SecrecyBar>("SecrecyBar");
        remove_choose_a_card = GetNodeOrNull<ChoiceRemoveCards>("RemoveChooseACard");

        if (hand != null) hand.CardActivated += _on_hand_card_activated;
        if (end_turn_button != null) end_turn_button.Pressed += _on_end_turn_pressed;
        if (view_deck_button != null) view_deck_button.Pressed += _on_view_deck_button_pressed;
        if (draw_pile != null) draw_pile.Pressed += _on_draw_pile_pressed;
        if (discard_pile != null) discard_pile.Pressed += _on_discard_pile_pressed;
        if (view_map_button != null) view_map_button.Pressed += _on_view_map_button_pressed;
        if (map != null) map.Chosen += _on_encounter_chosen_received;
        if (rewards != null) rewards.Chosen += _on_reward_card_chosen;
        if (remove_choose_a_card != null) remove_choose_a_card.Chosen += _on_remove_card_chosen;

        if (turn_announcer != null) turn_announcer.TotalDuration = turn_delay;
        _generate_starting_deck();

        // Debug: print deck / card-data export status so we can verify starting deck was populated
        GD.Print($"[MAIN DEBUG] gaslight_card_data set={(gaslight_card_data != null)} overcompensate_card_data set={(overcompensate_card_data != null)} false_lead_card_data set={(false_lead_card_data != null)}");
        GD.Print($"[MAIN DEBUG] deck playable size={deck.GetPlayableDeck().Size()}");

        // Hand position is set in Hand.tscn / Main.tscn — no runtime override needed.

        // Initialize UI deck widgets and populate the player's starting hand
        if (view_deck_button != null)
        {
            view_deck_button.Disabled = deck.GetPlayableDeck().Size() == 0;
            view_deck_button.deck = deck.GetPlayableDeck();
            view_deck_button.SetLabelDeckSize();
        }
        if (draw_pile != null)
        {
            draw_pile.deck = deck.GetPlayableDeck();
            draw_pile.Disabled = false;
            draw_pile.SetLabelDeckSize();
            draw_pile.deck.Shuffle();
        }
        if (discard_pile != null)
        {
            discard_pile.deck = new PlayableDeck();
            discard_pile.SetLabelDeckSize();
            discard_pile.Disabled = true;
        }

        // Show 6 random cards at the bottom as the starting hand
        GD.Print("[MAIN DEBUG] Beginning to deal starting hand...");
        _deal_starting_hand(6);
        turn_announcer?.Announce("Steal the secrets of sugar city!", turn_announcer.TotalDuration * 2.5f);

        _original_music_volume = GetMusicBusVolume();

        Input.SetCustomMouseCursor(GD.Load<Texture2D>("res://assets/images/ui/mouse_cursor.png"));
        map?.ReturnToMap();
        if (map != null)
        {
            var mb = map.GetNodeOrNull<Button>("BackButton");
            if (mb != null) mb.Visible = false;
        }
    }

    public override void _Process(double delta)
    {
        if (game_controller != null && !game_controller.is_running)
            return;
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("restart"))
            _restart_game();
        else if (@event.IsActionPressed("mouse_click_back") && deck_view_control != null && deck_view_control.Visible)
            deck_view_control.Back();
        else if (@event.IsActionPressed("mouse_click_back") && map != null && map.Visible && !game_won)
            map.Back();

        if (@event.IsActionPressed("mouse_click") || @event.IsActionPressed("mouse_click_back"))
        {
            // play ButtonSFX
        }
    }

    private bool _is_game_over()
    {
        if (player_character != null && player_character.health <= 0)
            game_controller?.Transition(GameController.GameState.GAME_OVER);
        else if (enemy_character != null && enemy_character.health <= 0)
            game_controller?.Transition(GameController.GameState.GAME_WON);

        var game_over = game_controller != null && game_controller.current_state == GameController.GameState.GAME_OVER;
        return game_over || (enemy_character != null && enemy_character.health <= 0);
    }

    private void _start_player_turn()
    {
        if (_is_game_over()) return;

        if (view_map_button != null) view_map_button.Disabled = true;
        if (view_deck_button != null) view_deck_button.Disabled = true;
        if (draw_pile != null) draw_pile.Disabled = true;
        if (discard_pile != null) discard_pile.Disabled = true;

        var t = turn_announcer?.Announce("Player Turn");
        if (t != null)
        {
            t.Finished += () =>
            {
                if (view_map_button != null) view_map_button.Disabled = false;
                if (view_deck_button != null) view_deck_button.Disabled = false;
                if (draw_pile != null) draw_pile.Disabled = false;
                if (discard_pile != null) discard_pile.Disabled = false;
            };
        }

        if (end_turn_button != null) end_turn_button.Disabled = true;
        game_controller?.Transition(GameController.GameState.PLAYER_TURN);
        player_character?.StartTurn();
        mana_orb?.FillUpAnimation();
        if (mana_orb != null && player_character != null) mana_orb.Label.Text = player_character.mana.ToString();
        _deal_to_hand();
    }

    private void _start_enemy_turn()
    {
        game_controller?.Transition(GameController.GameState.ENEMY_TURN);
        enemy_character?.StartTurn();
        Tween tween = null;

        switch (enemy_character_state)
        {
            case 0:
                enemy_character?.AddDefense((int)(0 * ascension_modifier));
                // play AttackActionSFX
                tween = enemy_character?.DealDamageAnimation();
                player_character?.TakeDamage((int)(3 * ascension_modifier));
                break;
            case 1:
                enemy_character?.AddDefense((int)(1 * ascension_modifier));
                // play AttackActionSFX
                tween = enemy_character?.DealDamageAnimation();
                player_character?.TakeDamage((int)(2 * ascension_modifier));
                break;
            case 2:
                enemy_character?.AddDefense((int)(2 * ascension_modifier));
                // play AttackActionSFX
                tween = enemy_character?.DealDamageAnimation();
                player_character?.TakeDamage((int)(1 * ascension_modifier));
                break;
        }

        enemy_character_state = (enemy_character_state + 1) % 3;
        var delay = turn_announcer != null ? turn_announcer.TotalDuration / 2.0f : turn_delay / 2.0f;
        if (!_is_game_over())
        {
            if (tween != null)
            {
                tween.TweenCallback(Callable.From(() => { } )).SetDelay(delay);
                tween.Finished += _start_player_turn;
            }
        }
        else
        {
            if (tween != null)
            {
                tween.TweenCallback(Callable.From(() => { } )).SetDelay(delay);
                tween.Finished += () =>
                {
                    if (_is_game_over()) return;
                    var t2 = turn_announcer?.Announce("Defeated!");
                    if (t2 != null) t2.Finished += () =>
                    {
                        if (map != null)
                        {
                            map.Visible = true;
                            var mb = map.GetNodeOrNull<Button>("BackButton");
                            if (mb != null) mb.Visible = false;
                        }
                    };
                };
            }
        }
    }

    private void _on_end_turn_pressed()
    {
        if (game_controller != null && game_controller.current_state != GameController.GameState.PLAYER_TURN) return;
        if (_is_game_over()) return;

        if (end_turn_button != null) end_turn_button.Disabled = true;
        if (view_map_button != null) view_map_button.Disabled = true;
        if (view_deck_button != null) view_deck_button.Disabled = true;
        if (draw_pile != null) draw_pile.Disabled = true;
        if (discard_pile != null) discard_pile.Disabled = true;
        _empty_hand_to_discard_pile();
        var t = turn_announcer?.Announce("Enemy Turn");
        if (t != null) t.Finished += _start_enemy_turn;
    }

    private void _empty_hand_to_discard_pile()
    {
        if (hand == null) return;
        foreach (object obj in hand.Empty())
        {
            PlayableCard playable_card = null;
            if (obj is PlayableCard pc) playable_card = pc;
            else
            {
                // obj may be a boxed Variant/Godot object; try to cast via 'as'
                playable_card = obj as PlayableCard;
            }
            if (playable_card == null) continue;
            playable_card.Visible = false;
            if (!playable_card.exhausted)
            {
                var cardToAdd = deck.GetCard(playable_card.id);
                discard_pile?.AddCardOnTop(cardToAdd);
            }
            if (discard_pile != null) discard_pile.Disabled = false;
        }
    }

    private void _on_hand_card_activated(PlayableCard playable_card)
    {
        if (rewards != null && rewards.Visible) return;
        if (_is_game_over()) return;

        var card_cost = playable_card.GetCost();
        if (player_character != null && card_cost > player_character.mana) return;

        var game_state = new Godot.Collections.Dictionary { ["actor"] = player_character, ["targets"] = new Godot.Collections.Array { enemy_character }, ["cost"] = card_cost };
        playable_card.Activate(game_state);
        _check_if_card_won_the_game();

        // Iterate as object to avoid pattern-matching directly on Godot Variant values.
        foreach (object actionObj in playable_card.actions)
        {
            // Prefer Action instances or RefCounted wrappers provided by resources
            if (actionObj is Action action)
            {
                if (action is DrawACardAction dac)
                {
                    _draw_a_card_to_hand(null, dac.number_of_cards_to_draw);
                }
                else if (action is ExhaustAction)
                {
                    playable_card.exhausted = true;
                }
                else if (action is ExhaustOtherRandomAction)
                {
                    // Pick random card safely
                    PlayableCard random_card = null;
                    if (hand != null && hand.cards.Count > 0)
                    {
                        var rng = new RandomNumberGenerator(); rng.Randomize();
                        int idx = (int)rng.RandiRange(0, hand.cards.Count - 1);
                        object maybe = hand.cards[idx];
                        random_card = maybe as PlayableCard;
                    }
                    if (random_card != null) random_card.exhausted = true;
                    if (random_card != null) hand.RemoveByEntity(random_card);
                }
                else if (action is RevealSecretAction rsa)
                {
                    secrecy_bar?.Update(rsa.num_secrets_revealed);
                }
                else if (action is HealAction heal)
                {
                    if (player_character != null) player_character.health += heal.num_heal;
                }

                continue;
            }

            // Some resources may provide RefCounted Action instances
            if (actionObj is RefCounted rc)
            {
                var a = rc as Action;
                if (a != null)
                {
                    if (a is DrawACardAction dac2)
                        _draw_a_card_to_hand(null, dac2.number_of_cards_to_draw);
                }
            }
        }

        if (playable_card.Card != null && playable_card.Card.type == CardData.Type.ATTACK)
        {
            // play AttackActionSFX
        }
        if (playable_card.Card != null && playable_card.Card.type == CardData.Type.DEFENSE)
        {
            // play ShieldSFX
        }

        player_character?.SpendMana(card_cost);
        if (mana_orb != null && player_character != null) mana_orb.Label.Text = player_character.mana.ToString();
        if (player_character != null && player_character.mana > 0) mana_orb?.SpendAnimation(); else mana_orb?.EmptyAnimation();

        hand.RemoveByEntity(playable_card);
        if (!playable_card.exhausted)
            discard_pile?.AddCardOnTop(deck.GetCard(playable_card.id));
        if (discard_pile != null) discard_pile.Disabled = discard_pile.deck.Size() <= 0;
    }

    private void _check_if_card_won_the_game()
    {
        var tween = CreateTween();
        var delay = turn_announcer != null ? turn_announcer.TotalDuration : turn_delay;
        tween.TweenCallback(Callable.From(() => { })).SetDelay(delay);
        tween.Finished += () =>
        {
            if (game_won) return;
            if (_is_game_over() && !game_won)
                rewards?.Activate(secrecy_bar != null && secrecy_bar.IsSecretRevealed());
            if (rewards != null && rewards.Visible)
            {
                _switch_music();
                game_won = true;
            }
        };
    }

    private void _restart_game()
    {
        game_won = false;
        secrecy_bar?.Restart();
        rewards_received = 0;
        if (map != null)
        {
            var mb = map.GetNodeOrNull<Button>("BackButton");
            if (mb != null) mb.Visible = true;
        }
        player_character?.SoftReset();
        player_character?.HealUpALittle();
        enemy_character?.HardReset();
        enemy_character_state = 0;
        hand?.Empty();
        if (mana_orb != null && player_character != null) mana_orb.Label.Text = player_character.mana.ToString();

        if (view_deck_button != null) { view_deck_button.Disabled = deck.GetPlayableDeck().Size() == 0; view_deck_button.deck = deck.GetPlayableDeck(); view_deck_button.SetLabelDeckSize(); }

        deck.Shuffle();
        if (draw_pile != null) { draw_pile.deck = deck.GetPlayableDeck(); draw_pile.Disabled = false; draw_pile.SetLabelDeckSize(); draw_pile.deck.Shuffle(); }

        if (discard_pile != null) { discard_pile.deck = new PlayableDeck(); discard_pile.SetLabelDeckSize(); discard_pile.Disabled = true; }

        if (rewards != null) rewards.Visible = false;
        if (game_over_color_rect != null) game_over_color_rect.Visible = false;

        var tween = CreateTween();
        var t = turn_announcer?.Announce("Battle start!");
        if (t != null) t.Finished += _start_player_turn;
        _fade_out();
    }

    private void _deal_to_hand()
    {
        // play DealToHandSFX
        var tween = CreateTween();
        if (player_character != null)
        {
            for (int i = 0; i < player_character.number_of_cards_to_be_dealt; i++)
                _draw_a_card_to_hand(tween, player_character.number_of_cards_to_be_dealt);
        }
        tween.TweenCallback(Callable.From(() => { if (end_turn_button != null) end_turn_button.Disabled = false; })).SetDelay(0);
    }

    private void _draw_a_card_to_hand(Tween tween, int cards_to_be_dealt)
    {
        if (tween == null) tween = CreateTween();
        _check_transfer_from_discard_to_draw_pile(cards_to_be_dealt);
        tween.TweenCallback(Callable.From(() => _draw_card_to_hand())).SetDelay(0.2f);
    }

    private void _on_view_deck_button_pressed()
    {
        _toggle_deck_view(deck.GetCards(), DeckViewControl.Type.DECK);
    }

    private void _on_draw_pile_pressed()
    {
        _toggle_deck_view(draw_pile.deck.ToArray(), DeckViewControl.Type.DRAW_PILE);
    }

    private void _on_view_map_button_pressed()
    {
        map?.Enable(_is_game_over());
    }

    private void _on_discard_pile_pressed()
    {
        _toggle_deck_view(discard_pile.deck.ToArray(), DeckViewControl.Type.DISCARD_PILE);
    }

    private void _toggle_deck_view(Godot.Collections.Array deckArr, DeckViewControl.Type type)
    {
        game_controller?.TogglePauseAndResume();
        if (deck_view_control != null) deck_view_control.Visible = !deck_view_control.Visible;
        deck_view_control?.DeckViewWindow.DisplayCardList(deckArr);
        deck_view_control?.SetType(type);
        deck_view_control?.PlayAudio(type, game_controller != null && game_controller.is_running);
    }

    private void _generate_starting_deck()
    {
        // Add gaslight cards
        for (int i = 0; i < 5; i++)
        {
            GD.Print($"[MAIN DEBUG] _generate_starting_deck: adding gaslight_card_data set={(gaslight_card_data != null)}");
            if (gaslight_card_data != null) deck.AddCard(gaslight_card_data);
            GD.Print($"[MAIN DEBUG] deck size now={deck.GetPlayableDeck().Size()}");
        }
        // Add overcompensate cards
        for (int i = 0; i < 5; i++)
        {
            GD.Print($"[MAIN DEBUG] _generate_starting_deck: adding overcompensate_card_data set={(overcompensate_card_data != null)}");
            if (overcompensate_card_data != null) deck.AddCard(overcompensate_card_data);
            GD.Print($"[MAIN DEBUG] deck size now={deck.GetPlayableDeck().Size()}");
        }
        // Add false_lead
        for (int i = 0; i < 1; i++)
        {
            GD.Print($"[MAIN DEBUG] _generate_starting_deck: adding false_lead_card_data set={(false_lead_card_data != null)}");
            if (false_lead_card_data != null) deck.AddCard(false_lead_card_data);
            GD.Print($"[MAIN DEBUG] deck size now={deck.GetPlayableDeck().Size()}");
        }
    }

    private void _check_transfer_from_discard_to_draw_pile(int cards_to_be_dealt)
    {
        if (draw_pile != null && draw_pile.GetNumberOfCards() < cards_to_be_dealt)
        {
            var number_of_cards = discard_pile.GetNumberOfCards();
            discard_pile.deck.Shuffle();
            for (int i = 0; i < number_of_cards; i++)
                draw_pile.AddCardOnBottom(discard_pile.Draw());
            if (draw_pile != null) draw_pile.Disabled = false;
            if (discard_pile != null) discard_pile.Disabled = true;
        }
        discard_pile?.SetLabelDeckSize();
    }

    private void _draw_card_to_hand()
    {
        GD.Print("[MAIN DEBUG] _draw_card_to_hand: draw_pile deck size before draw = " + (draw_pile != null ? draw_pile.GetNumberOfCards().ToString() : "<no draw_pile>"));
        var card_with_id = draw_pile?.Draw();
        if (card_with_id == null)
        {
            GD.Print("[MAIN DEBUG] draw_pile.Draw() returned null (no cards available or draw_pile is null)");
            return;
        }

        draw_pile.SetLabelDeckSize();
        var playable_card = playable_card_scene.Instantiate() as PlayableCard;
        AddChild(playable_card);
        playable_card.Visible = false;
        if (card_with_id.Card != null)
            playable_card.LoadCardData(card_with_id.Card);
        playable_card.id = card_with_id.Id;
        try { playable_card.GlobalPosition = hand.GlobalPosition; } catch { playable_card.Position = Vector2.Zero; }
        RemoveChild(playable_card);
        hand.AddCard(playable_card);

        if (draw_pile.GetNumberOfCards() == 0)
            draw_pile.Disabled = true;
    }

    // Deal N cards immediately into the player's hand (used at game start)
    private void _deal_starting_hand(int n)
    {
        if (hand == null || playable_card_scene == null || draw_pile == null) return;
        GD.Print($"[MAIN DEBUG] _deal_starting_hand: draw_pile size={draw_pile.GetNumberOfCards()}");

        for (int i = 0; i < n; i++)
        {
            var card_with_id = draw_pile.Draw();
            GD.Print($"[MAIN DEBUG] _deal_starting_hand: loop {i}, draw_pile.Draw() returned={(card_with_id != null ? "CardWithID(" + card_with_id.Id + ")" : "null")}");
            if (card_with_id == null) break;

            var playable_card = playable_card_scene.Instantiate() as PlayableCard;
            if (playable_card == null)
            {
                GD.PrintErr("[MAIN DEBUG] _deal_starting_hand: playable_card_scene.Instantiate() returned null");
                continue;
            }

            AddChild(playable_card);
            playable_card.Visible = false;
            if (card_with_id.Card != null)
                playable_card.LoadCardData(card_with_id.Card);
            playable_card.id = card_with_id.Id;
            try { playable_card.GlobalPosition = hand.GlobalPosition; } catch { playable_card.Position = Vector2.Zero; }
            RemoveChild(playable_card);
            hand.AddCard(playable_card);
            GD.Print($"[MAIN DEBUG] _deal_starting_hand: added playable_card id={playable_card.id}; hand.cards.Count={hand.cards.Count}");
        }

        // Update UI deck counters
        draw_pile.SetLabelDeckSize();
        discard_pile?.SetLabelDeckSize();

        // Debug output: viewport and hand/card positions to help track visibility issues
        var rect = GetViewport().GetVisibleRect();
        GD.Print($"[DEBUG] viewport size = {rect.Size}");
        GD.Print($"[DEBUG] hand global position = {hand.GlobalPosition}, hand position = {hand.Position}");
        GD.Print($"[DEBUG] hand.cards count = {hand.cards.Count}");
        for (int i = 0; i < hand.cards.Count; i++)
        {
            object obj = hand.cards[i];
            string typeName = obj != null ? obj.GetType().ToString() : "<null>";
            bool isPlayable = obj is PlayableCard;
            bool isNode = obj is Node;
            GD.Print($"[DEBUG] card[{i}] storedType={typeName} isPlayable={isPlayable} isNode={isNode}");
            var pc = obj as PlayableCard;
            if (pc != null)
            {
                GD.Print($"[DEBUG] card[{i}] id={pc.id} visible={pc.Visible} global_pos={pc.GlobalPosition} pos={pc.Position}");
            }
        }
    }

    private void _on_encounter_chosen_received(Encounter encounter)
    {
        var encData = encounter.GetCharacterData();
        if (encData != null)
        {
            enemy_character?.LoadData(encData);
            secrecy_bar?.Initialize(encData);
        }
        else
        {
            GD.PrintErr("Encounter has no CharacterData resource assigned or type not bound yet.");
        }
        if (map != null)
        {
            map.Visible = false;
            var mb = map.GetNodeOrNull<Button>("BackButton");
            if (mb != null) mb.Disabled = false;
        }
        _switch_music();
        _restart_game();
    }

    private void _on_remove_card_chosen(PlayableCard playable_card)
    {
        if (playable_card != null && playable_card.card_data != null)
        {
            deck.RemoveCardByData(playable_card.card_data);
            if (view_deck_button != null) { view_deck_button.deck = deck.GetPlayableDeck(); view_deck_button.SetLabelDeckSize(); }
        }
    }

    private void _on_reward_card_chosen(PlayableCard playable_card)
    {
        if (playable_card != null && playable_card.card_data != null)
        {
            deck.AddCard(playable_card.card_data);
            if (view_deck_button != null) { view_deck_button.deck = deck.GetPlayableDeck(); view_deck_button.SetLabelDeckSize(); }
        }

        rewards_received += 1;
        if (rewards_received < (rewards != null ? rewards.num_rewards : 0)) return;

        if (rewards != null) rewards.Visible = false;
        map?.ReturnToMap();
        if (secrecy_bar != null && secrecy_bar.IsSecretRevealed()) map?.Disable(enemy_character);
        if (map != null)
        {
            var mb2 = map.GetNodeOrNull<Button>("BackButton");
            if (mb2 != null) mb2.Visible = false;
        }

        if (map != null && map.IsAllEncountersDefeated())
        {
            map.EnableAllEncounters();
            ascension_level += 1;
            remove_choose_a_card?.Activate(deck);
            if (map != null)
            {
                var ascLabel = map.GetNodeOrNull<Label>("AscensionLabel");
                if (ascLabel != null)
                {
                    ascLabel.Visible = true;
                    ascLabel.Text = "Ascension: " + ascension_level.ToString();
                }
            }

            foreach (object encObj in map.GetAllEncounters())
            {
                var enc = encObj as Encounter;
                if (enc == null) continue;
                var cd = enc.GetCharacterData();
                if (cd != null)
                {
                    cd.MaxHealth = (int)(cd.MaxHealth * ascension_modifier);
                    cd.NumSecrets = (int)(cd.NumSecrets * ascension_modifier);
                }
            }

            ascension_modifier *= ascension_modifier;
        }
    }

    private void _fade_out()
    {
        if (fade_in_color_rect != null)
        {
            fade_in_color_rect.Visible = true;
            fade_in_color_rect.Modulate = new Color(fade_in_color_rect.Modulate.R, fade_in_color_rect.Modulate.G, fade_in_color_rect.Modulate.B, 1.0f);
            var tween = CreateTween();
            tween.SetTrans(Tween.TransitionType.Quad);
            tween.SetEase(Tween.EaseType.In);
            tween.TweenProperty(fade_in_color_rect, "modulate:a", 0.0f, 0.5f);
        }
    }

    private void _switch_music()
    {
        // Simplified: play/stop music with fade; exact SFX nodes need to be looked up in the scene
        var main_music = GetNodeOrNull<AudioStreamPlayer>("MainMusic");
        var map_music = GetNodeOrNull<AudioStreamPlayer>("MapMusic");
        var tween = CreateTween();
        tween.SetTrans(Tween.TransitionType.Sine);
        var duration = 0.5f;

        if (main_music != null && main_music.Playing)
        {
            tween.SetEase(Tween.EaseType.In);
            tween.TweenMethod(new Callable(this, nameof(_set_music_bus_volume)), _original_music_volume, -80f, duration);
            tween.TweenCallback(Callable.From(() => map_music?.Play()));
            tween.TweenCallback(Callable.From(() => main_music?.Stop()));
            tween.SetEase(Tween.EaseType.Out);
            tween.TweenMethod(new Callable(this, nameof(_set_music_bus_volume)), -80f, _original_music_volume, duration);
        }
        else
        {
            tween.SetEase(Tween.EaseType.In);
            tween.TweenMethod(new Callable(this, nameof(_set_music_bus_volume)), _original_music_volume, -80f, duration);
            tween.TweenCallback(Callable.From(() => main_music?.Play()));
            tween.TweenCallback(Callable.From(() => map_music?.Stop()));
            tween.SetEase(Tween.EaseType.Out);
            tween.TweenMethod(new Callable(this, nameof(_set_music_bus_volume)), -80f, _original_music_volume, duration);
        }
    }

    private void _set_music_bus_volume(float from, float to, float duration)
    {
        int idx = AudioServer.GetBusIndex("Music");
        if (idx < 0)
        {
            GD.PrintErr($"Music audio bus not found (GetBusIndex returned {idx}). Falling back to bus 0.");
            idx = 0;
        }
        AudioServer.SetBusVolumeDb(idx, to);
    }

    private float GetMusicBusVolume()
    {
        int idx = AudioServer.GetBusIndex("Music");
        if (idx < 0)
        {
            GD.PrintErr($"Music audio bus not found (GetBusIndex returned {idx}). Falling back to bus 0.");
            idx = 0;
        }
        return AudioServer.GetBusVolumeDb(idx);
    }
}

