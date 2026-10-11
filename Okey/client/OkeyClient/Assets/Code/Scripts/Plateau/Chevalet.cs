using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Code.Scripts.SignalR.Packets;
using Unity.VisualScripting;
using UnityEngine;

public class Chevalet : MonoBehaviour
{
    public static readonly GameObject[] Placeholders = new GameObject[28]; // Tableau des 28 Placeholders (la grille)

    public TuileData[,] Tuiles2D = new TuileData[2, 14];
    public TuileData[,] TuilesPack = new TuileData[2, 14];
    public readonly Stack<Tuile> _pileGauche = new Stack<Tuile>();
    private Stack<Tuile> _pileDroite = new Stack<Tuile>();
    public readonly Stack<Tuile> _pilePioche = new Stack<Tuile>();
    public static GameObject PileGauchePlaceHolder;
    public static GameObject PilePiochePlaceHolder;

    public static GameObject PileDroitePlaceHolder;
    public static GameObject JokerPlaceHolder;

    public static GameObject PileHautDroitePlaceHolder;
    public static GameObject PileHautGauchePlaceHolder;

    public static Dictionary<string, Sprite> spritesDic = new Dictionary<string, Sprite>();

    public static bool PiocheIsVide = false;

    public bool IsJete { get; set; }
    public TuilePacket TuileJete { get; set; }

    public static bool IsPiochee { get; set; }

    public PiochePacket TuilePiochee = null;

    public bool IsTireeHasard { get; set; }
    public TuilePacket TuileTireeHasard = null;

    public static bool neverReceivedChevalet = true;

    public static Chevalet Instance { get; set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.Log("Destroying duplicate instance of Chevalet.");
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
            // DontDestroyOnLoad(this.gameObject);
            Debug.Log("Chevalet instance set.");
        }
    }

    public void Init()
    {
        var sprites = Resources.LoadAll<Sprite>("Tiles");

        for (var i = 0; i < 13; i++)
        {
            if (!spritesDic.ContainsKey($"Black_{i + 1}"))
            {
                spritesDic.TryAdd($"Black_{i + 1}", sprites[i]);
            }
        }

        for (var i = 13; i < 26; i++)
        {
            if (!spritesDic.ContainsKey($"Black_{i + 1}"))
            {
                spritesDic.TryAdd($"Blue_{(i + 1) - 13}", sprites[i]);
            }
        }

        if (!spritesDic.ContainsKey("Fake Joker_1"))
        {
            spritesDic.TryAdd("Fake Joker_1", sprites[26]);
        }

        if (!spritesDic.ContainsKey("Fake Joker_2"))
        {
            spritesDic.TryAdd("Fake Joker_2", sprites[27]);
        }

        for (var i = 28; i < 41; i++)
        {
            if (!spritesDic.ContainsKey($"Green_{(i + 1) - 28}"))
            {
                spritesDic.TryAdd($"Green_{(i + 1) - 28}", sprites[i]);
            }
        }

        if (!spritesDic.ContainsKey("Pioche"))
        {
            spritesDic.TryAdd($"Pioche", sprites[41]);
        }

        for (var i = 42; i < 55; i++)
        {
            if (!spritesDic.ContainsKey($"Red_{(i + 1) - 42}"))
            {
                spritesDic.TryAdd($"Red_{(i + 1) - 42}", sprites[i]);
            }
        }

        this.InitPlaceholders();
        this.InitializeBoardFromTuiles();
        this.TuileJete = null;
        this.IsJete = false;

        this.IsTireeHasard = false;
        this.TuileTireeHasard = null;

        IsPiochee = false;
        this.TuilePiochee = null;
    }

    private void Start()
    {
        this.InitPlaceholders();
        this.Awake();
        //this.InitializeBoardFromPlaceholders();
        //this.InitializeBoardFromTuiles();

        //PrintTuilesArray();
    }

    private void InitPlaceholders()
    {
        // Remplir le tableau avec les placeholders dans la scene
        for (var i = 1; i <= 28; i++) //i commence a 1 car le premier placeholder est "PlaceHolder1"
        {
            var placeholder = GameObject.Find("PlaceHolder" + i);
            if (placeholder != null)
            {
                Placeholders[i - 1] = placeholder; // Mettre le placeholder dans le tableau
            }
            else
            {
                Debug.LogError("PlaceHolder" + i + " not found!");
            }
        }

        //ToDo : Instancier les piles en fonction de ce qu'on reçoit du Back
        PileGauchePlaceHolder = GameObject.Find("PileGauchePlaceHolder");
        PileDroitePlaceHolder = GameObject.Find("PileDroitePlaceHolder");
        PilePiochePlaceHolder = GameObject.Find("PiochePlaceHolder");
        JokerPlaceHolder = GameObject.Find("Okey");
        PileHautDroitePlaceHolder = GameObject.Find("PileHautDroitePlaceHolder");
        PileHautGauchePlaceHolder = GameObject.Find("PileHautGauchePlaceHolder");
        //this._pileGauche.Push(
        //    PileGauchePlaceHolder.transform.GetChild(0).gameObject.GetComponent<Tuile>()
        //);
        //this._pileGauche.Push(
        //    PileGauchePlaceHolder.transform.GetChild(0).gameObject.GetComponent<Tuile>()
        //);
        //this._pilePioche.Push(
        //    PilePiochePlaceHolder.transform.GetChild(0).gameObject.GetComponent<Tuile>()
        //);

        if (PileGauchePlaceHolder.transform.childCount > 0)
        {
            PileGauchePlaceHolder
                .transform.GetChild(0)
                .gameObject.GetComponent<Tuile>()
                .SetIsInStack(true);
            PileGauchePlaceHolder
                .transform.GetChild(0)
                .gameObject.GetComponent<Tuile>()
                .SetIsDeplacable(false);
        }

        PilePiochePlaceHolder.transform.gameObject.GetComponent<Tuile>().SetIsDeplacable(false);

        PileDroitePlaceHolder
            .transform.GetChild(0)
            .gameObject.GetComponent<Tuile>()
            .SetIsDeplacable(false);
        JokerPlaceHolder
            .transform.GetChild(0)
            .gameObject.GetComponent<Tuile>()
            .SetIsDeplacable(false);
        /*
        PilePiochePlaceHolder
            .transform.GetChild(0)
            .gameObject.GetComponent<Tuile>()
            .SetIsDeplacable(false);
        PilePiochePlaceHolder
            .transform.GetChild(0)
            .gameObject.GetComponent<Tuile>()
            .SetIsInPioche(true);*/
    }

    public GameObject ClosestPlaceholder(Vector3 Position)
    {
        GameObject closestPlaceholder = null;
        var closestDistance = Mathf.Infinity;

        // Loop through placeholders using absolute positions
        foreach (var placeholder in Placeholders)
        {
            if (placeholder != null)
            {
                var distance = Vector3.Distance(Position, placeholder.transform.position); // Absolute position
                if (distance < closestDistance)
                {
                    closestPlaceholder = placeholder;
                    closestDistance = distance;
                }
            }
        }

        // Check specific piles using absolute positions
        var distancePileDroite = Vector3.Distance(
            Position,
            PileDroitePlaceHolder.transform.position
        ); // Absolute position

        var distanceJoker = Vector3.Distance(Position, JokerPlaceHolder.transform.position);

        // Determine closest placeholder based on distance and tile number
        if (distancePileDroite < closestDistance && GetTilesNumber() == 15)
        {
            closestDistance = distancePileDroite;
            closestPlaceholder = PileDroitePlaceHolder;
        }
        else if (
            (distanceJoker < closestDistance && GetTilesNumber() == 15)
            && (distanceJoker < distancePileDroite)
        )
        {
            closestDistance = distanceJoker;
            closestPlaceholder = JokerPlaceHolder;
        }
        else if (
            Position.y > 0
            && GetTilesNumber() == 15 /* et peut gagner*/
        )
        {
            closestPlaceholder = PilePiochePlaceHolder;
        }
        return closestPlaceholder;
    }

    public void UpdateTiles(GameObject Placeholder)
    {
        // Vérifier si le placeholder a des enfants (des tuiles)
        if (Placeholder.transform.childCount > 1)
        {
            // Déterminer le numéro du placeholder actuel
            var currentPlaceholderNumber = GetPlaceholderNumber(Placeholder.name);
            var indexTab = currentPlaceholderNumber - 1;

            // Vérifier s'il y a un placeholder à droite et s'il n'est pas plein
            if (
                currentPlaceholderNumber < 29
                && Placeholders[(indexTab + 1) % 28].transform.childCount <= 1
            )
            {
                // Déplacer la première tuile du placeholder actuel vers le placeholder à droite
                var tile = Placeholder.transform.GetChild(0).gameObject;
                tile.GetComponent<Tuile>().AttachToPlaceholder(Placeholders[(indexTab + 1) % 28]);

                // Appeler récursivement la fonction UpdateTiles sur le placeholder à droite
                this.UpdateTiles(Placeholders[(indexTab + 1) % 28]);
            }
        }
    }

    public void Draw(bool PiocheDroite)
    {
        if (
            GetTilesNumber() == 14 /* et c'est mon tour*/
        )
        {
            if (PiocheDroite)
            {
                PileGauchePlaceHolder
                    .transform.GetChild(0)
                    .gameObject.GetComponent<Tuile>()
                    .SetIsDeplacable(true);
                PileGauchePlaceHolder
                    .transform.GetChild(0)
                    .gameObject.GetComponent<Tuile>()
                    .SetIsInStack(false);
                this._pileGauche.Pop();
                if (this._pileGauche.Count > 0)
                {
                    var newChild = Instantiate(
                        this._pileGauche.Peek().gameObject,
                        PileGauchePlaceHolder.transform
                    );
                    newChild.transform.localPosition = Vector3.zero;
                    newChild.GetComponent<Tuile>().SetIsInStack(true);
                    newChild.GetComponent<Tuile>().SetIsDeplacable(false);
                }
            }
            else
            {
                PilePiochePlaceHolder
                    .transform.GetChild(0)
                    .gameObject.GetComponent<Tuile>()
                    .SetIsDeplacable(true);
                PilePiochePlaceHolder
                    .transform.GetChild(0)
                    .gameObject.GetComponent<Tuile>()
                    .SetIsInPioche(false);
                this._pilePioche.Pop();
                if (this._pilePioche.Count > 0)
                {
                    var newChild = Instantiate(
                        this._pilePioche.Peek().gameObject,
                        PilePiochePlaceHolder.transform
                    );
                    newChild.transform.localPosition = Vector3.zero;
                    newChild.GetComponent<Tuile>().SetIsInStack(true);
                    newChild.GetComponent<Tuile>().SetIsDeplacable(false);
                }
            }
        }
    }

    public void ThrowTile(Tuile Tuile)
    {
        this._pileDroite.Push(Tuile);

        Debug.Log($"Tuile recue {Tuile.GetCouleur()}, {Tuile.GetValeur()}");
        var tuileData = this.GetTuilePacketFromChevalet(Tuile, false);
        Debug.Log($"{tuileData.Y}, {tuileData.X}");
        this.IsJete = true;
        this.TuileJete = tuileData;
    }

    public void PiocheTile(bool isCenter)
    {
        Debug.Log("Il faut piocher une tuile");
        if (isCenter)
        {
            this.TuilePiochee = new PiochePacket { Centre = true, Defausse = false };
        }
        else
        {
            this.TuilePiochee = new PiochePacket { Centre = false, Defausse = true };
        }
        IsPiochee = true;
    }

    public void ThrowTileToWin(Tuile Tuile)
    {
        var tuileData = this.GetTuilePacketFromChevalet(Tuile, true);
        Debug.Log($"{tuileData.Y}, {tuileData.X}, {tuileData.gagner}");
        this.IsJete = true;
        this.TuileJete = tuileData;
    }

    public TuilePacket GetTuilePacketFromChevalet(Tuile T, bool gain)
    {
        if (T != null)
        {
            for (var x = 0; x < 2; x++)
            {
                for (var y = 0; y < 14; y++)
                {
                    if (T.GetCouleur() == "M" && T.GetValeur() == 1)
                    {
                        Debug.Log("On veut jeter le okey");

                        if (
                            this.TuilesPack[x, y].couleur.Equals("M", StringComparison.Ordinal)
                            && this.TuilesPack[x, y].num == 0
                        )
                        {
                            return new TuilePacket
                            {
                                X = "" + y,
                                Y = "" + x,
                                gagner = gain
                            };
                        }
                    }
                    else
                    {
                        if (
                            this.TuilesPack[x, y]
                                .couleur.Equals(T.GetCouleur(), StringComparison.Ordinal)
                            && this.TuilesPack[x, y].num == T.GetValeur()
                        )
                        {
                            return new TuilePacket
                            {
                                X = "" + y,
                                Y = "" + x,
                                gagner = gain
                            };
                        }
                    }
                }
            }
        }
        Debug.Log("On jete une tuile nulle.");
        return new TuilePacket();
    }

    // Fonction auxiliaire pour extraire le numéro du placeholder à partir de son nom
    public static int GetPlaceholderNumber(string Name)
    {
        var numberString = Name.Substring(11);
        int number;
        int.TryParse(numberString, out number);
        return number;
    }

    private void InitializeBoardFromPlaceholders()
    {
        for (var i = 0; i < Placeholders.Length; i++)
        {
            var x = i / 14; // Calculate the row based on index.
            var y = i % 14; // Calculate the column based on index.

            var placeholder = Placeholders[i];

            //Debug.Log($"Checking placeholder {i}, Child count: {placeholder.transform.childCount}");

            if (placeholder.transform.childCount > 0)
            {
                // Get the first child of the placeholder
                var child = placeholder.transform.GetChild(0).gameObject;

                if (child.TryGetComponent<SpriteRenderer>(out var childSpriteRenderer))
                {
                    // note: "color_value" should be used for naming the sprites
                    var properties = childSpriteRenderer.sprite.name.Split('_');
                    //Debug.Log(properties[0] + properties[1]);

                    if (properties.Length == 2)
                    {
                        var couleur = this.ConvertToFrontendColorToBackendEnumName(properties[0]);
                        var num = int.Parse(properties[1]);
                        var isJoker = properties[0] == "FakeJoker";
                        this.Tuiles2D[x, y] = new TuileData(couleur, num, isJoker);
                        var tuilePlaceHolder = placeholder.GetComponent<Tuile>();
                        tuilePlaceHolder.SetCouleur(couleur.ToString());
                        tuilePlaceHolder.SetValeur(num);
                        tuilePlaceHolder.SetIsJoker(isJoker);
                        //this.tuilesPack[x, y] = new TuileData(couleur, num, isJoker);
                        //Debug.Log("["+x+"]"+"["+y+"]"+" "+this.tuiles2D[x, y].couleur+ " " +this.tuiles2D[x, y].num+" "+this.tuiles2D[x, y].isJoker);
                    }
                    else if (
                        childSpriteRenderer.sprite.name.Equals("Pioche", StringComparison.Ordinal)
                    )
                    {
                        var tuile = child.GetComponent<Tuile>();

                        var tuilePlaceHolder = placeholder.GetComponent<Tuile>();
                        tuilePlaceHolder.SetCouleur(tuile.GetCouleur());
                        tuilePlaceHolder.SetValeur(tuile.GetValeur());

                        this.Tuiles2D[x, y] = new TuileData(
                            FromStringToCouleurTuile(tuilePlaceHolder.GetCouleur()),
                            tuilePlaceHolder.GetValeur(),
                            tuilePlaceHolder.GetIsJoker()
                        );
                        var mat = new Material(Shader.Find("Sprites/Default"))
                        {
                            color = new Color(
                                0.9529411764705882f,
                                0.9411764705882353f,
                                0.8156862745098039f
                            )
                        };
                        childSpriteRenderer.material = mat;
                        childSpriteRenderer.sprite = spritesDic[
                            FromTuileToSpriteName(this.Tuiles2D[x, y])
                        ];
                    }
                    else
                    {
                        // If the name does not contain both color and value, log an error or set as null
                        Debug.LogError(
                            $"Child sprite of placeholder at index {i} does not have a properly formatted name."
                        );
                        this.Tuiles2D[x, y] = null;
                        var tuilePlaceHolder = placeholder.GetComponent<Tuile>();
                        tuilePlaceHolder.SetCouleur(null);
                        tuilePlaceHolder.SetValeur(0);
                        //this.tuilesPack[x, y] = null;
                    }
                }
                else
                {
                    // If there is no SpriteRenderer component, set the corresponding array position to null
                    this.Tuiles2D[x, y] = null;
                    var tuilePlaceHolder = placeholder.GetComponent<Tuile>();
                    tuilePlaceHolder.SetCouleur(null);
                    tuilePlaceHolder.SetValeur(0);
                }
            }
            else //empty placeholder
            {
                // If the placeholder is empty, set the corresponding array position to null
                this.Tuiles2D[x, y] = null;
                var tuilePlaceHolder = placeholder.GetComponent<Tuile>();
                tuilePlaceHolder.SetCouleur(null);
                tuilePlaceHolder.SetValeur(0);
            }
        }
    }

    public static string FromTuileToSpriteName(TuileData Tuile)
    {
        if (
            Tuile.couleur.Equals("M", StringComparison.Ordinal)
            || Tuile.couleur.Equals("X", StringComparison.Ordinal)
        )
        {
            return "Fake Joker_1";
        }
        var name = "";
        switch (Tuile.couleur)
        {
            case "B":
                name = "Blue_";
                break;
            case "R":
                name = "Red_";
                break;
            case "N":
                name = "Black_";
                break;
            case "J":
                name = "Green_";
                break;
            case "V":
                name = "Green_";
                break;
        }

        name += Tuile.num;
        return name;
    }

    public static CouleurTuile FromStringToCouleurTuile(string CouleurString)
    {
        CouleurTuile coul;
        switch (CouleurString)
        {
            case "J":
                coul = CouleurTuile.J;
                break;
            case "V":
                coul = CouleurTuile.J;
                break;
            case "N":
                coul = CouleurTuile.N;
                break;
            case "R":
                coul = CouleurTuile.R;
                break;
            case "B":
                coul = CouleurTuile.B;
                break;
            case "M":
                coul = CouleurTuile.M;
                break;
            case "X":
                coul = CouleurTuile.M;
                break;
            default:
                Debug.LogError(CouleurString);
                throw new Exception();
                break;
        }
        return coul;
    }

    public void InitializeBoardFromTuiles()
    {
        for (var i = 0; i < Placeholders.Length; i++)
        {
            var x = i / 14;
            var y = i % 14;
            var placeholder = Placeholders[i];
            if (this.Tuiles2D[x, y] != null)
            {
                if (placeholder.transform.childCount > 0)
                {
                    Destroy(placeholder.transform.GetChild(0).gameObject);
                }
                var childObject = new GameObject("SpriteChild");
                childObject.transform.SetParent(placeholder.transform);
                var spriteRen = childObject.AddComponent<SpriteRenderer>();
                var mat = new Material(Shader.Find("Sprites/Default"))
                {
                    color = new Color(0.9529411764705882f, 0.9411764705882353f, 0.8156862745098039f)
                };
                spriteRen.material = mat;
                spriteRen.sprite = spritesDic[FromTuileToSpriteName(this.Tuiles2D[x, y])];
                spriteRen.sortingOrder = 3;
                var transform1 = spriteRen.transform;
                transform1.localPosition = new Vector3(0, 0, 0);
                transform1.localScale = new Vector3(1, 1, 1);
                childObject.AddComponent<Tuile>();
                var boxCollider2D = childObject.AddComponent<BoxCollider2D>();
                boxCollider2D.size = new Vector2((float)0.875, (float)1.25);

                placeholder.GetComponent<Tuile>().SetValeur(this.Tuiles2D[x, y].num);
                placeholder.GetComponent<Tuile>().SetCouleur(this.Tuiles2D[x, y].couleur);
                placeholder.GetComponent<Tuile>().SetIsJoker(this.Tuiles2D[x, y].isJoker);
            }
            else
            {
                if (placeholder.transform.childCount > 0)
                {
                    Destroy(placeholder.transform.GetChild(0).gameObject);
                }
            }
        }
    }

    private CouleurTuile ConvertToFrontendColorToBackendEnumName(string FrontendColor)
    {
        return FrontendColor.ToLower() switch
        {
            "yellow" => CouleurTuile.J,
            "green" => CouleurTuile.J,
            "black" => CouleurTuile.N,
            "red" => CouleurTuile.R,
            "blue" => CouleurTuile.B,

            // other cases still needed
            "fakejoker" => CouleurTuile.M,
            _ => CouleurTuile.M, // the okey
        };
    }

    private (int, int) ConvertPlaceHolderNumberToMatrixCoordinates(int PlaceholderNumber)
    {
        //conversion du numero du place holder 1->28 en coordonnées dans la matrice [2][14]
        var x = (PlaceholderNumber - 1) / 14;
        var y = (PlaceholderNumber - 1) % 14;
        return (x, y);
    }

    public void UpdateMatrixAfterMovement(
        GameObject PreviousPlaceHolder,
        GameObject NextPlaceholder
    )
    {
        //cas de deplacement a l'interieur du chevalet : Chevalet -> Chevalet
        var chevaletFront = GameObject.Find("ChevaletFront");
        if (
            PreviousPlaceHolder.transform.IsChildOf(chevaletFront.transform)
            && NextPlaceholder.transform.IsChildOf(chevaletFront.transform)
        )
        {
            if (!PreviousPlaceHolder.name.Equals(NextPlaceholder.name, StringComparison.Ordinal))
            {
                var tuile = PreviousPlaceHolder.GetComponent<Tuile>();
                var nt = NextPlaceholder.GetComponent<Tuile>();
                nt.SetCouleur(tuile.GetCouleur());
                nt.SetValeur(tuile.GetValeur());
                nt.SetIsJoker(tuile.GetIsJoker());

                tuile.SetCouleur(null);
                tuile.SetValeur(0);
                this.InitializeBoardFromPlaceholders(); // il faut re parcourir le chevalets pour recuperer les nouvelles position car il y'a le decalage des tuiles
            }
            this.InitializeBoardFromPlaceholders(); // il faut re parcourir le chevalets pour recuperer les nouvelles position car il y'a le decalage des tuiles
        }
        //cas de tirage : pioche ou pile gauche -> Chevalet
        else if (
            (
                PreviousPlaceHolder == PileGauchePlaceHolder
                || PreviousPlaceHolder == PilePiochePlaceHolder
            ) && NextPlaceholder.transform.IsChildOf(chevaletFront.transform)
        )
        {
            //ajouter la piece pioché au chevalet
            this.InitializeBoardFromPlaceholders(); // il faut re parcourir le chevalets car quand on pioche on peut la mettre entre 2 tuiles et ca crée un decalage
            var tuile = PreviousPlaceHolder.GetComponent<Tuile>();
            var nt = NextPlaceholder.GetComponent<Tuile>();

            nt.SetCouleur(tuile.GetCouleur());
            nt.SetValeur(tuile.GetValeur());
            nt.SetIsJoker(tuile.GetIsJoker());

            if (PreviousPlaceHolder == PilePiochePlaceHolder)
            {
                Debug.Log("Ici");
                if (this._pilePioche.Count > 0)
                {
                    this._pilePioche.Pop();
                    if (this._pilePioche.Count > 0)
                    {
                        PilePiochePlaceHolder
                            .GetComponent<Tuile>()
                            .SetCouleur(this._pilePioche.Peek().GetCouleur());
                        PilePiochePlaceHolder
                            .GetComponent<Tuile>()
                            .SetValeur(this._pilePioche.Peek().GetValeur());
                    }
                }
                this.PiocheTile(true);
            }
            else
            {
                Debug.Log("La");
                this._pileGauche.Pop();
                if (this._pileGauche.Count > 0)
                {
                    PileGauchePlaceHolder
                        .GetComponent<Tuile>()
                        .SetCouleur(this._pileGauche.Peek().GetCouleur());
                    PileGauchePlaceHolder
                        .GetComponent<Tuile>()
                        .SetValeur(this._pileGauche.Peek().GetValeur());
                }
                this.PiocheTile(false);
            }
            //Faudra parler a lequipe du backend pour savoir si ca leur suffit la matrice mis a jour et le contenu des defausses ou ils veulent exactement la piece pioché
        }
        //cas de jet : Chevalet -> pile droite / tentative de gain
        else if (
            PreviousPlaceHolder.transform.IsChildOf(chevaletFront.transform)
            && (NextPlaceholder == PileDroitePlaceHolder || NextPlaceholder == JokerPlaceHolder)
        )
        {
            //enlever la piece jeté du chevalet
            var prvPhPos = this.ConvertPlaceHolderNumberToMatrixCoordinates(
                GetPlaceholderNumber(PreviousPlaceHolder.name)
            );

            //Debug.Log($"tuile[{prv_ph_pos.Item1}][{prv_ph_pos.Item1}] a été jeté");
            Debug.Log(
                ""
                    + this.Tuiles2D[prvPhPos.Item1, prvPhPos.Item2].couleur
                    + this.Tuiles2D[prvPhPos.Item1, prvPhPos.Item2].num
                    + this.Tuiles2D[prvPhPos.Item1, prvPhPos.Item2].isJoker
            );

            if (NextPlaceholder == PileDroitePlaceHolder)
            {
                if (IsJete)
                {
                    this.Tuiles2D[prvPhPos.Item1, prvPhPos.Item2] = null;

                    var tuile = PreviousPlaceHolder.GetComponent<Tuile>();

                    var nt = NextPlaceholder.GetComponent<Tuile>();
                    nt.SetCouleur(tuile.GetCouleur());
                    nt.SetValeur(tuile.GetValeur());
                    nt.SetIsJoker(tuile.GetIsJoker());
                    tuile.SetCouleur(null);
                    tuile.SetValeur(0);
                    for (var i = 0; i < PileDroitePlaceHolder.transform.childCount; i++)
                    {
                        PileDroitePlaceHolder
                            .transform.GetChild(i)
                            .GetComponent<SpriteRenderer>()
                            .sortingOrder = i;
                    }
                }
            }
            else
            {
                var tuile = PreviousPlaceHolder.GetComponent<Tuile>();
                var nt = NextPlaceholder.GetComponent<Tuile>();
                nt.SetCouleur(tuile.GetCouleur());
                nt.SetValeur(tuile.GetValeur());
                nt.SetIsJoker(tuile.GetIsJoker());
            }

            //Faudra parler a lequipe du backend pour savoir si ca leur suffit la matrice mis a jour et le contenu des defausses ou ils veulent exactement la piece jeté
        }
        else //cas derreur
        {
            Debug.LogError("error updating matrix after movement");

            Debug.Log($"Source: {PreviousPlaceHolder.name}, Destination: {NextPlaceholder.name}");
        }

        this.Print2DMatrix();
    }

    public void MoveFromChevaletToDefausse(TuilePacket tuile)
    {
        var xS = int.Parse(tuile.X);
        var yS = int.Parse(tuile.Y);
        var xChevalet = 0;
        var yChevalet = 0;
        var tuileServ = this.TuilesPack[xS, yS];

        // Trouver les coordonnes actuelles sur le chevalet
        TuileData tuileChevalet;
        for (var xC = 0; xC < 2; xC++)
        {
            for (var yC = 0; yC < 14; yC++)
            {
                if (this.Tuiles2D[xC, yC] != null)
                {
                    if (
                        (this.Tuiles2D[xC, yC].num == tuileServ.num)
                        && (
                            tuileServ.couleur.Equals(
                                this.Tuiles2D[xC, yC].couleur,
                                StringComparison.Ordinal
                            )
                        )
                    )
                    {
                        tuileChevalet = this.Tuiles2D[xC, yC];
                        xChevalet = xC;
                        yChevalet = yC;
                        int coordPlaceHolder = 0;

                        if (xChevalet == 0)
                        {
                            coordPlaceHolder = yChevalet;
                        }
                        else
                        {
                            coordPlaceHolder = yChevalet + 15;
                        }

                        var sourceplaceHolder = Placeholders[coordPlaceHolder];

                        sourceplaceHolder.GetComponent<Tuile>().SetCouleur(null);
                        sourceplaceHolder.GetComponent<Tuile>().SetValeur(0);
                        var spriteToDelete = sourceplaceHolder.transform.GetChild(0);
                        spriteToDelete.transform.SetParent(PileDroitePlaceHolder.transform);

                        var transform1 = spriteToDelete.transform;
                        transform1.localPosition = new Vector3(0, 0, 0);

                        for (var i = 0; i < PileDroitePlaceHolder.transform.childCount; i++)
                        {
                            PileDroitePlaceHolder
                                .transform.GetChild(i)
                                .GetComponent<SpriteRenderer>()
                                .sortingOrder = i;
                        }

                        this.Tuiles2D[xC, yC] = null;
                    }
                }
            }
        }
    }

    public void SetTuileCentre(TuileStringPacket tuile)
    {
        if (tuile.numero != null)
        {
            var tuileData = new TuileData(
                FromStringToCouleurTuile(tuile.Couleur),
                int.Parse(tuile.numero),
                false
            );

            var childObject = new GameObject("SpriteChild");
            childObject.transform.SetParent(JokerPlaceHolder.transform);
            var spriteRen = childObject.AddComponent<SpriteRenderer>();
            var mat = new Material(Shader.Find("Sprites/Default"))
            {
                color = new Color(0.9529411764705882f, 0.9411764705882353f, 0.8156862745098039f)
            };
            spriteRen.material = mat;
            Debug.Log(FromTuileToSpriteName(tuileData));
            spriteRen.sprite = spritesDic[FromTuileToSpriteName(tuileData)];
            spriteRen.sortingOrder = 3;
            var transform1 = spriteRen.transform;
            transform1.localPosition = new Vector3(0, 0, 0);
            transform1.localScale = new Vector3(1, 1, 1);
            childObject.AddComponent<Tuile>();
            var boxCollider2D = childObject.AddComponent<BoxCollider2D>();
            boxCollider2D.size = new Vector2((float)0.875, (float)1.25);
        }
    }

    public void Print2DMatrix() //Really useful to visualize tiles placement matrix
    {
        // Initialize a string to store the table
        var table = "";

        for (var x = 0; x < 2; x++) // Rows
        {
            for (var y = 0; y < 14; y++) // Columns
            {
                if (this.Tuiles2D[x, y] != null)
                {
                    table +=
                        $"| {this.Tuiles2D[x, y].couleur} {this.Tuiles2D[x, y].num} {this.Tuiles2D[x, y].isJoker} ";
                }
                else
                {
                    table += "| Vide ";
                }
            }
            table += "|\n                    "; // End of the row
        }

        // Print the table
        Debug.Log(table);
    }

    public void SetTuiles(TuileData[,] Tuiles)
    {
        this.Tuiles2D = Tuiles;
    }

    //public void SetTuile(int x, int y, TuileData tuile)
    //{
    //    tuiles2D[x, y] = tuile;
    //}

    //// get Tuile de l'index donnee
    //public TuileData GetTuile(int x, int y)
    //{
    //    if (x >= 0 && x < 2 && y >= 0 && y < 14)
    //    {
    //        return tuiles2D[x, y];
    //    }
    //    else
    //    {
    //        Debug.LogError("GetTuile: Index out of bounds");
    //        return null;
    //    }
    //}

    public TuileData[,] GetTuilesArray()
    {
        return this.Tuiles2D;
    }

    private void PrintTuilesArray()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Board State:");

        for (var x = 0; x < this.Tuiles2D.GetLength(0); x++)
        {
            for (var y = 0; y < this.Tuiles2D.GetLength(1); y++)
            {
                var tile = this.Tuiles2D[x, y];
                if (tile != null)
                {
                    sb.AppendLine(
                        $"Tile at [{x},{y}]: Color = {tile.couleur}, Number = {tile.num}, IsJoker = {tile.isJoker}"
                    );
                }
                else
                {
                    sb.AppendLine($"Tile at [{x},{y}]: None");
                }
            }
        }

        Debug.Log(sb.ToString());
    }

    private static int GetTilesNumber()
    {
        var num = 0;
        foreach (var placeholder in Placeholders)
        {
            if (placeholder.transform.childCount == 1)
            {
                num++;
            }
        }
        return num;
    }

    /*private List<TuileData> TrierTuilesParNumero(List<TuileData> tuiles)
    {
        var tuilesTriees = tuiles.OrderBy(tuile => tuile.num).ToList();
        return tuilesTriees;
    }

    private (List<TuileData>, List<TuileData>) TriCouleur(List<TuileData> tuiles)
    {
        List<TuileData> tuilesTriees = this.TrierTuilesParNumero(tuiles);
        List<TuileData> series = new List<TuileData>();
        List<TuileData> nonSeries = new List<TuileData>();

        // Variables pour suivre la série actuelle
        List<TuileData> currentSeries = new List<TuileData>();

        for (int i = 0; i < tuilesTriees.Count; i++)
        {
            // Ajouter la tuile actuelle à la série en cours
            currentSeries.Add(tuilesTriees[i]);

            // Vérifier si la tuile suivante n'est pas consécutive ou s'il n'y a plus de tuiles
            if (
                i == tuilesTriees.Count - 1
                || tuilesTriees[i + 1].Numero != tuilesTriees[i].Numero + 1
            )
            {
                // Si la série est valide (au moins 3 tuiles), ajouter à la liste des séries
                if (currentSeries.Count >= 3)
                {
                    series.AddRange(currentSeries);
                    series.Add(null); // peut causer des erreus de debordements
                }
                else
                {
                    // Sinon, ajouter à la liste des non-séries
                    nonSeries.AddRange(currentSeries);
                }

                // Réinitialiser la série en cours
                currentSeries = new List<TuileData>();
            }
        }


        return (series, nonSeries);
    }

    private (List<TuileData>, List<TuileData>) triChiffre(List<TuileData> nonSerie)
    {
        // Dictionnaire pour stocker les tuiles par numéro
        Dictionary<int, List<TuileData>> tuilesParNumero = new Dictionary<int, List<TuileData>>();

        // Parcourir les tuiles et les regrouper par numéro
        foreach (var tuile in nonSerie)
        {
            if (!tuilesParNumero.ContainsKey(tuile.Numero))
            {
                tuilesParNumero[tuile.Numero] = new List<TuileData>();
            }
            tuilesParNumero[tuile.Numero].Add(tuile);
        }

        List<TuileData> memesNumeros = new List<TuileData>();
        List<TuileData> restantes = new List<TuileData>();

        // Vérifier les groupes de tuiles pour des couleurs différentes
        foreach (var entry in tuilesParNumero)
        {
            var groupe = entry.Value;
            if (groupe.Select(t => t.Couleur).Distinct().Count() > 1)
            {
                memesNumeros.AddRange(groupe);
                memesNumeros.Add(null); // peut causer des erreurs
            }
            else
            {
                restantes.AddRange(groupe);
            }
        }

        return (memesNumeros, restantes);
    }

    private List<List<TuileData>> GroupByCouleur(TuileData[,] Matrice)
    {
        List<TuileData> rouges = new List<TuileData>();
        List<TuileData> bleus = new List<TuileData>();
        List<TuileData> noirs = new List<TuileData>();
        List<TuileData> jaunes = new List<TuileData>();
        List<TuileData> okeys = new List<TuileData>();

        for (int i = 0; i < Matrice.GetLength(0); i++)
        {
            for (int j = 0; j < Matrice.GetLength(1); j++)
            {
                TuileData tuile = Matrice[i, j];
                if (tuile != null)
                {
                    switch (tuile.Couleur.ToLower())
                    {
                        case "rouge":
                            rouges.Add(tuile);
                            break;
                        case "bleu":
                            bleus.Add(tuile);
                            break;
                        case "noir":
                            noirs.Add(tuile);
                            break;
                        case "jaune":
                            jaunes.Add(tuile);
                            break;
                        case "multicolor":
                            okeys.Add(tuile);
                            break;
                    }
                }
            }
        }

        return new List<List<TuileData>> { rouges, bleus, noirs, jaunes, okeys };
    }

    public void TriChevalet()
    {
        List<List<TuileData>> groupedByColor = this.GroupByCouleur(this.Tuiles2D); // 5 lists

        List<TuileData> serieFinale = new List<TuileData>();
        List<TuileData> resteDeMemeCouleurFinale = new List<TuileData>();

        for (int i = 0; i < 4; i++)
        {
            List<TuileData> serieMemeCouleur = new List<TuileData>();
            List<TuileData> resteDeMemeCouleur = new List<TuileData>();
            (serieMemeCouleur, resteDeMemeCouleur) = this.TriCouleur(groupedByColor[i]);

            serieFinale.AddRange(serieMemeCouleur);
            resteDeMemeCouleurFinale.AddRange(resteDeMemeCouleur);
        }

        List<TuileData> serieDansReste = new List<TuileData>();
        List<TuileData> resteDansReste = new List<TuileData>();

        (serieDansReste, resteDansReste) = this.triChiffre(resteDeMemeCouleurFinale);
        List<TuileData> okeys = groupedByColor[4];
        resteDansReste.AddRange(okeys);

        // on ajoute toutes les bonnes series ici.
        serieFinale.AddRange(serieDansReste);

        int i = 0, j = 0; // pour le parcours de chevalet

        foreach (var tuile in serieFinale)
        {
            while (i < this.Tuiles2D.Count && j < this.Tuiles2D[i].Count && this.Tuiles2D[i][j] != null)
            {
                j++;
                if (j >= this.Tuiles2D[i].Count)
                {
                    j = 0;
                    i++;
                }
            }

            if (i < this.Tuiles2D.Count && j < this.Tuiles2D[i].Count)
            {
                this.Tuiles2D[i][j] = tuile;
            }
        }

        for (i = 0; i < this.Tuiles2D.Count; i++)
        {
            for (j = 0; j < this.Tuiles2D[i].Count; j++)
            {
                if (this.Tuiles2D[i][j] == null)
                {
                    // Ajout des tuiles dans resteDansReste a la fin
                    if (resteDansReste.Count > 0)
                    {
                        this.Tuiles2D[i][j] = resteDansReste[0];
                        resteDansReste.RemoveAt(0);
                    }
                }
            }
        }
    }

    public void TriEnCouple()
    {
        // Dictionnaire pour compter les tuiles par couleur et numéro
        var tuileCount = new Dictionary<(string, int), List<TuileData>>();

        // Parcourir le tableau 2D et remplir le dictionnaire
        for (int i = 0; i < Tuiles2D.GetLength(0); i++)
        {
            for (int j = 0; j < Tuiles2D.GetLength(1); j++)
            {
                var tuile = Tuiles2D[i, j];
                if (tuile != null)
                {
                    var key = (tuile.Couleur, tuile.Numero);
                    if (!tuileCount.ContainsKey(key))
                    {
                        tuileCount[key] = new List<TuileData>();
                    }
                    tuileCount[key].Add(tuile);
                }
            }
        }

        // Réinitialiser Tuiles2D
        Tuiles2D = new TuileData[2, 14];
        int row = 0, col = 0;

        List<TuileData> remainingTiles = new List<TuileData>();

        // Parcourir le dictionnaire et placer les couples dans Tuiles2D
        foreach (var kvp in tuileCount)
        {
            var tuileList = kvp.Value;
            while (tuileList.Count >= 2)
            {
                if (col >= Tuiles2D.GetLength(1))
                {
                    col = 0;
                    row++;
                    if (row >= Tuiles2D.GetLength(0)) return; // Arrêter si on dépasse les limites
                }

                // Ajouter le couple
                Tuiles2D[row, col] = tuileList[0];
                Tuiles2D[row, col + 1] = tuileList[1];
                // Supprimer les deux tuiles ajoutées
                tuileList.RemoveAt(0);
                tuileList.RemoveAt(0);

                // Mettre null après le couple
                if (col + 2 < Tuiles2D.GetLength(1))
                {
                    Tuiles2D[row, col + 2] = null;
                    col += 3;
                }
                else
                {
                    col = 0;
                    row++;
                    if (row < Tuiles2D.GetLength(0))
                    {
                        Tuiles2D[row, col] = null;
                        col++;
                    }
                }
            }

            // Ajouter les tuiles restantes au remainingTiles
            remainingTiles.AddRange(tuileList);
        }

        // Ajouter les tuiles restantes à la fin de Tuiles2D
        foreach (var tuile in remainingTiles)
        {
            if (col >= Tuiles2D.GetLength(1))
            {
                col = 0;
                row++;
                if (row >= Tuiles2D.GetLength(0)) return; // Arrêter si on dépasse les limites
            }
            Tuiles2D[row, col] = tuile;
            col++;
        }
    }*/
}


/*
    to keep just in case :
    public static Tuile[] GetTilesPlacementInChevaletTab()
    {
        // Creation d'un tableau pour stocker le placement courant des tuiles sur le chevalet
        Tuile[] TilesArray = new Tuile[placeholders.Length];

        //loop sur les placeholders
        foreach (GameObject placeholder in placeholders)
        {
            int index = Array.IndexOf(placeholders, placeholder);

            if (placeholder.transform.childCount > 0)
            { // le placeholder a un child -> il contient une tuile -> on la met dans le tableau avec l'index du placeholder parent
                GameObject child = placeholder.transform.GetChild(0).gameObject;
                Tuile tuile = child.GetComponent<Tuile>();

                if (tuile != null)
                {
                    // Place the Tuile in the 1D array
                    TilesArray[index] = tuile;
                }
                else
                {
                    Debug.LogError("Error : placeholder has a child that isn't a Tile");
                }
            }
            else // si le placeholder n'a aucun child -> il est vide -> on met null a l'index correspondant au placeholder dans le tableau
            {
                TilesArray[index] = null;
            }
        }

        return TilesArray;
    }

    public static void PrintTilesArrayForTest()
    {
        // Get the tiles placement array using the method
        Tuile[] TilesArray = GetTilesPlacementInChevaletTab();

        // Check if TilesArray is not null
        if (TilesArray != null)
        {
            // Loop through the TilesArray to print tile number and color
            for (int i = 0; i < TilesArray.Length; i++)
            {
                if (TilesArray[i] != null)
                {
                    // Check if GetValeur() and GetCouleur() are not null
                    if (
                        TilesArray[i].GetValeur().ToString() != null
                        && TilesArray[i].GetCouleur().ToString() != null
                    ) // supposons que les valeurs sont init a 0
                    {
                        Debug.Log(
                            "Tile Number "
                                + (i + 1)
                                + " :"
                                + TilesArray[i].GetValeur().ToString()
                                + ", Tile Color: "
                                + TilesArray[i].GetCouleur().ToString()
                        );
                    }
                    else
                    {
                        Debug.Log("Tile at index " + i + " has null values.");
                    }
                }
                else
                {
                    Debug.Log("Tile at index " + i + " is null.");
                }
            }
        }
        else
        {
            Debug.Log("TilesArray is null.");
        }
    }
    */
