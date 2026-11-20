# O Guardião do Farol — Guia do Projeto

Jogo 2D top-down de simulação casual. Você é o taverneiro da taverna **O Farol** e precisa juntar **100 moedas em 5 dias** atendendo heróis aposentados, ou a taverna será fechada pelo cobrador Valdemar Ferrete.

O projeto já está **montado, jogável e com pixel art própria**: o taverneiro, 7 heróis diferentes, piso de madeira, paredes de pedra, balcão, barril, fogão, mesas, cadeiras, porta, ícones dos produtos, balão de pedido e o farol do menu. Os PNGs (16 PPU) estão em `Assets/_Project/Art/Generated` e são gerados pelo menu **Guardiao do Farol > Gerar e Aplicar Pixel Art**. Para usar a sua arte, basta substituir um PNG mantendo o mesmo nome. Para começar, abra `Assets/_Project/Scenes/MainMenu.unity` e aperte **Play**.

| Arquivo | O que é |
| ------- | ------- |
| `Taverneiro.png` | Jogador (16×24, pivô nos pés) |
| `Heroi_Borin/Lyra/Tavio/Aldric/Mirela/Grom/Celestia.png` | Guerreiro, maga, arqueiro, paladino, ladina, bárbaro e clériga |
| `Heroi_Generico.png` | Sprite padrão do prefab Customer |
| `Tile_Madeira_A/B.png`, `Tile_Pedra.png` | Piso e parede do Tilemap |
| `Mesa`, `Cadeira`, `Balcao`, `Barril`, `Fogao`, `Porta` | Móveis da taverna |
| `Icone_Cerveja/Carne/Sopa.png`, `Balao.png` | Ícones dos pedidos e balão |
| `Farol.png` | Ilustração do menu inicial |

---

## Balanceamento (resumo)

| Produto | Onde pegar | Preço |
| ------- | ---------- | ----- |
| Cerveja | Barril | 5 moedas |
| Sopa | Fogão (lado direito) | 6 moedas |
| Carne | Fogão (lado esquerdo) | 8 moedas |

| Métrica | Valor |
| ------- | ----- |
| Clientes na campanha | 21 (3 + 3 + 5 + 5 + 5) |
| Pagamento médio por cliente (pedido aleatório) | ≈ 6,33 moedas |
| Máximo teórico (21 pedidos de carne) | 168 moedas |
| Esperado atendendo todos os 21 | ≈ 133 moedas |
| Pior caso atendendo todos (21 cervejas) | 105 moedas, então **a vitória nunca é impossível** |
| Atendimentos necessários para 100 moedas | ≈ **16 de 21** (entre 13 e 20, conforme os pedidos sorteados) |

Na média, o jogador pode perder uns 5 clientes. Perdendo 7 ou mais, a derrota fica muito provável.

---

## A. Configuração do projeto

| Item | Valor |
| ---- | ----- |
| Unity | 6000.6.8f1 (Unity 6.x; qualquer Unity 6 LTS funciona) |
| Template | Universal 2D (URP 2D) |
| Input | **Edit > Project Settings > Player > Active Input Handling = Input System Package (New)** (ou *Both*). O código usa `UnityEngine.InputSystem.Keyboard`. |
| Pacotes usados | Input System, Universal RP, 2D Tilemap, uGUI + TextMeshPro (já vêm no template). Nenhum pacote externo. |
| TextMeshPro | Recursos essenciais importados em `Assets/TextMesh Pro` (Window > TextMeshPro > Import TMP Essential Resources). |
| Pixels por unidade | **16 PPU** (1 tile = 16×16 px = 1 unidade). Importe seus sprites com PPU 16, Filter Mode **Point**, Compression **None**. |
| Câmera | Ortográfica, tamanho **7,5**, posição (0, 0, -10), fixa, top-down. Opcional: adicionar *Pixel Perfect Camera* (Assets PPU 16). |
| Ordenação top-down | `Assets/Settings/Renderer2D.asset` → **Transparency Sort Mode = Custom Axis (0, 1, 0)**. Objetos mais baixos na tela ficam na frente. |
| Sorting Layers | `Default`, `Floor` (piso, paredes, cadeiras), `World` (móveis e personagens), `Overhead` (balões, barras, destaques). |
| Canvas | Screen Space Overlay, Canvas Scaler *Scale With Screen Size*, resolução de referência **1920×1080**, Match 0,5. |
| EventSystem | `EventSystem` + **Input System UI Input Module** (não use o Standalone Input Module). |
| Cenas no build | `MainMenu` (índice 0) e `Game` (índice 1). |

---

## B. Estrutura de pastas

```
Assets/
├── _Project/
│   ├── Animations/                  (seus Animator Controllers / clips — opcional)
│   ├── Art/
│   │   ├── Characters/              (sprites dos heróis e do taverneiro)
│   │   ├── Icons/                   (ícones de Cerveja, Carne e Sopa)
│   │   ├── Props/                   (balcão, barril, fogão, mesas, cadeiras, porta)
│   │   ├── Generated/               pixel art gerada (personagens, tiles, móveis, ícones)
│   │   ├── Placeholders/            PH_Square.png, PH_Circle.png, PH_Outline.png
│   │   └── Tiles/                   Tile_PisoA.asset, Tile_PisoB.asset, Tile_Parede.asset
│   ├── Audio/
│   │   ├── Music/
│   │   └── SFX/
│   ├── Data/
│   │   └── CampanhaGuardiao.asset   (os 5 dias, clientes, falas e finais)
│   ├── Prefabs/
│   │   ├── AudioManager.prefab
│   │   └── Customer.prefab
│   ├── Scenes/
│   │   ├── MainMenu.unity
│   │   └── Game.unity
│   └── Scripts/
│       ├── Audio/AudioManager.cs
│       ├── Core/GameManager.cs
│       ├── Core/DayManager.cs
│       ├── Customers/CustomerController.cs
│       ├── Customers/CustomerManager.cs
│       ├── Data/CampaignData.cs
│       ├── Data/ProductType.cs
│       ├── Dialogue/DialogueManager.cs
│       ├── Economy/EconomyManager.cs
│       ├── Interaction/Interactable.cs
│       ├── Interaction/ProductStation.cs
│       ├── Interaction/Table.cs
│       ├── Orders/OrderManager.cs
│       ├── Player/CharacterAnimator.cs
│       ├── Player/PlayerController.cs
│       ├── Player/PlayerInteraction.cs
│       ├── UI/MainMenuController.cs
│       ├── UI/UIManager.cs
│       ├── Editor/GuardiaoProjectBuilder.cs   (montador automático — só no Editor)
│       └── Editor/PixelArtLibrary.cs          (gera e aplica a pixel art — só no Editor)
├── Settings/   (URP, Renderer2D — do template)
└── TextMesh Pro/
```

---

## C. Hierarquia das cenas

### MainMenu
```
Main Camera            Camera (ortho), AudioListener, UniversalAdditionalCameraData
AudioManager           (prefab) AudioManager, 2× AudioSource
EventSystem            EventSystem, InputSystemUIInputModule
Canvas                 Canvas, CanvasScaler, GraphicRaycaster
├── Background         Image
├── Farol              Image (círculo dourado)
├── Titulo             TextMeshProUGUI "O Guardião do Farol"
├── Subtitulo          TextMeshProUGUI
├── BotaoJogar         Image, Button, Outline └── Texto (TMP)
├── BotaoCreditos      Image, Button, Outline └── Texto (TMP)
├── BotaoSair          Image, Button, Outline └── Texto (TMP)
├── Controles          TextMeshProUGUI
└── Creditos           Image (fundo escuro) └── Caixa └── Titulo, Texto, BotaoVoltar
MainMenu               MainMenuController (Play, Credits, Quit, Credits Panel, Close Credits Button)
```

### Game
```
Main Camera            Camera (ortho 7,5), AudioListener, UniversalAdditionalCameraData
Global Light 2D        Light2D (Global)
AudioManager           (prefab)
EventSystem            EventSystem, InputSystemUIInputModule
Grid                   Grid
├── Piso               Tilemap, TilemapRenderer (Floor, 0)
└── Paredes            Tilemap, TilemapRenderer (Floor, 1), TilemapCollider2D
Cenario
├── Porta              BoxCollider2D (impede o jogador de sair) └── Visual (SpriteRenderer)
├── Balcao             BoxCollider2D └── Visual, Tampo
├── Barril             CircleCollider2D (sólido), CircleCollider2D (trigger), ProductStation (Cerveja)
│   └── Visual, IconeProduto, Destaque
└── Fogao              BoxCollider2D └── Visual, Brasa
    ├── Estacao_Carne  CircleCollider2D (trigger), ProductStation (Carne) └── IconeProduto, Destaque
    └── Estacao_Sopa   CircleCollider2D (trigger), ProductStation (Sopa)  └── IconeProduto, Destaque
Waypoints
├── Porta_Spawn        (onde os clientes aparecem e somem)
├── Entrada
├── Corredor
└── Corredor_Mesa_1 … Corredor_Mesa_5
Mesas
└── Mesa_1 … Mesa_5    BoxCollider2D (sólido), BoxCollider2D (trigger), Table
    └── Visual, Caneca, Assento, Cadeira, Destaque
Taverneiro             Rigidbody2D, BoxCollider2D, CharacterAnimator, PlayerController, PlayerInteraction
└── Body, Avental, ItemCarregado (SpriteRenderer, Overhead)
Clientes               (pai dos clientes gerados em tempo de execução)
Sistemas
├── GameManager        GameManager
├── DayManager         DayManager
├── EconomyManager     EconomyManager
├── OrderManager       OrderManager
├── CustomerManager    CustomerManager
└── DialogueManager    DialogueManager
Canvas                 Canvas, CanvasScaler, GraphicRaycaster, UIManager
├── HUD
│   ├── BarraSuperior  (HorizontalLayoutGroup) └── Dia, Moedas, Tempo, Atendidos
│   ├── ItemCarregado  └── Icone (Image), Texto
│   ├── IndicadorInteracao └── Texto
│   └── Aviso          └── Texto
├── Dialogo            └── Nome, Fala, Dica
├── ResumoDoDia        └── Caixa └── Titulo, Texto, BotaoProximoDia
├── Vitoria            └── Titulo, Texto, BotaoJogarNovamente, BotaoMenu
└── Derrota            └── Titulo, Texto, BotaoJogarNovamente, BotaoMenu
```

### Prefab Customer
```
Customer               CharacterAnimator, CustomerController
├── Body               SpriteRenderer (World)  ← sprite do herói
├── OrderBubble        └── BubbleBackground, OrderIcon (Overhead)
└── PatienceBar        └── Background, FillPivot └── Fill (Overhead)
```

---

## D. Scripts

Os 18 scripts estão em `Assets/_Project/Scripts/`, no namespace `GuardiaoDoFarol`:

| Script | Responsabilidade |
| ------ | ---------------- |
| `GameManager` | Estado geral (Playing / Dialogue / DaySummary / Ended), pausa via `Time.timeScale`, finais, troca de cenas. |
| `DayManager` | Introdução do dia, cronômetro do expediente, encerramento, resumo, próximo dia. |
| `CustomerManager` | Fila de clientes do dia, chegadas em intervalos, reserva de mesas livres. |
| `CustomerController` | Caminhada por waypoints, pedido, paciência, pagamento, desistência e saída. |
| `OrderManager` | Catálogo dos 3 produtos (nome, ícone, cor, preço), sorteio e validação de entregas. |
| `EconomyManager` | Moedas totais, moedas do dia e meta. |
| `DialogueManager` | Caixa de diálogo com nome, efeito de digitação, fila de diálogos e pausa. |
| `UIManager` | HUD, item carregado, indicador de interação, avisos, resumo, vitória e derrota. |
| `AudioManager` | Música em loop e efeitos com volumes separados; tudo opcional. |
| `PlayerController` | Movimento com Rigidbody2D (WASD / setas). |
| `PlayerInteraction` | Alvo mais próximo, destaque, E/Espaço, item carregado (um por vez). |
| `Interactable` | Base abstrata de tudo que é interagível. |
| `ProductStation` | Barril/fogão: pegar, trocar ou devolver um produto. |
| `Table` | Mesa: ocupante, caminho de waypoints, entrega. |
| `CharacterAnimator` | Ponte opcional para Animator (MoveX, MoveY, Speed, IsSitting) e flip do sprite. |
| `CampaignData` | ScriptableObject com os 5 dias, os heróis, as falas e os textos dos finais. |
| `MainMenuController` | Tela de início: Jogar, Créditos (com Voltar/Esc) e Sair. |
| `GuardiaoProjectBuilder` (Editor) | Menu **Guardiao do Farol > Montar Projeto Completo**: recria tudo automaticamente. |

**Regra de encerramento do dia:** quando o tempo acaba, nenhum cliente novo entra (os que não chegaram contam como perdidos) e o dia termina quando os clientes que já estão na taverna forem atendidos ou desistirem. Se todos forem resolvidos antes, o dia termina mais cedo.

**Item errado:** levar o produto errado não é aceito (aparece um aviso) e o item continua na mão. Na estação, **E** troca o item ou, se for o mesmo produto, devolve.

**Diálogos:** E/Espaço/Enter completa o texto ou avança; Esc fecha. O jogo fica pausado enquanto o diálogo está aberto.

---

## E. Configuração pelo Inspector (para trocar placeholders pelos seus assets)

Tudo já está ligado pelo montador. Para usar a sua arte, troque **somente sprites/clipes**, sem mexer no código:

**Tilemap.** Crie tiles do seu tileset (Tile Palette) e pinte sobre `Grid/Piso` e `Grid/Paredes`. Os tiles de parede precisam de *Collider Type = Grid* ou *Sprite* (o `TilemapCollider2D` já está em `Paredes`). Deixe a abertura da porta livre (células x = -1 e 0, y = -7).

**Taverneiro.** Em `Taverneiro/Body`, troque o Sprite e ponha a cor em branco. Apague `Avental` se não precisar. Para animar, adicione um `Animator` no `Body` e arraste-o para o campo *Animator* do `CharacterAnimator` do Taverneiro. Parâmetros opcionais: `MoveX`, `MoveY`, `Speed` (float) e `IsSitting` (bool).

**Heróis.** No prefab `Customer`, troque o sprite de `Body`, que serve de padrão para todos. Para um sprite por herói, abra `Data/CampanhaGuardiao.asset` → Days → Customers → **Sprite** e use **Tint** branco.

**Ícones dos produtos.** Em `Sistemas/OrderManager` → *Products*, defina *Icon* para Cerveja, Carne e Sopa e *Icon Color* = branco. Os ícones aparecem no balão, sobre a cabeça, no HUD e sobre as estações.

**Móveis.** Troque o Sprite dos filhos `Visual` de `Balcao`, `Barril`, `Fogao`, `Mesa_N` e `Porta`. Se o tamanho mudar, ajuste os `BoxCollider2D`/`CircleCollider2D` do objeto pai: o sólido fica com *Is Trigger* desligado; o trigger maior é a área de interação.

**Áudio.** Abra o prefab `Prefabs/AudioManager` e arraste os clipes: Menu Music, Tavern Music, Footstep, Pickup, Deliver, Coin, Dialogue Open, Customer Enter, Victory, Defeat, Button Click. Os volumes ficam em Music Volume, Sfx Volume e Footstep Volume. Todos os campos podem ficar vazios.

**Colliders e Rigidbody2D.** O `Taverneiro` usa Rigidbody2D *Dynamic*, Gravity 0, Freeze Rotation (o script reforça isso) e um `BoxCollider2D` nos pés. Os clientes **não têm colliders**: andam por waypoints e não empurram o jogador.

**Spawn e waypoints.** `Waypoints/Porta_Spawn` é onde o cliente aparece e some, ligado em `CustomerManager` → *Door Point*. Cada `Mesa_N` → `Table` → *Path From Door* = [Entrada, Corredor, Corredor_Mesa_N] e *Seat Point* = `Mesa_N/Assento`. Mova esses Transforms à vontade; as linhas aparecem como gizmos.

**Nova mesa.** Duplique uma `Mesa_N`, crie um novo `Corredor_Mesa_X`, atualize o *Path From Door* e adicione a mesa na lista *Tables* do `CustomerManager`.

**Barril e fogão.** Em `ProductStation`, *Product* define o que é entregue, *Station Name* é o texto do indicador, *Product Icon Renderer* (opcional) mostra o ícone e *Highlight* (opcional) é o contorno exibido quando o jogador está perto.

**Botões.** São ligados por código. Basta que os campos do `UIManager` (Next Day Button, Victory/Defeat Play Again, Menu) e do `MainMenuController` (Play, Quit) apontem para os botões. **Não** adicione funções no OnClick pelo Inspector, ou elas rodariam duas vezes.

**Campos que podem ficar vazios.** Todos os AudioClips, *Highlight*, *Product Icon Renderer*, *Animator*, *Customers Parent*, *Interaction Origin*, *Sprite* dos heróis e *Campaign* (sem asset, a campanha padrão do código é usada).

**Para recriar tudo do zero:** use o menu **Guardiao do Farol > Montar Projeto Completo**. Isso **sobrescreve** as cenas `MainMenu` e `Game` e o prefab `Customer`; a campanha, o prefab `AudioManager` e os tiles existentes são preservados.

---

## F. Configuração da campanha

Arquivo: `Assets/_Project/Data/CampanhaGuardiao.asset`. Ele é editável no Inspector, inclusive as falas. O botão direito no asset → *Restaurar conteúdo padrão* recupera os valores originais.

| Dia | Clientes | Expediente | 1ª chegada | Intervalo | Paciência | Narrativa |
| --- | -------- | ---------- | ---------- | --------- | --------- | --------- |
| 1 | 3 | 100 s | 2 s | 10–14 s | 50 s | Apresentação: Borin, Lyra, Tavio |
| 2 | 3 | 100 s | 2 s | 9–13 s | 45 s | Aldric, Mirela, Grom |
| 3 | 5 | 140 s | 2 s | 8–12 s | 40 s | A carta do Coletor Real; Mirela revela Valdemar; a dívida de 100 moedas |
| 4 | 5 | 140 s | 2 s | 7–10 s | 35 s | Aviso de fechamento pregado na porta |
| 5 | 5 | 140 s | 2 s | 6–9 s | 30 s | Último dia antes do pôr do sol |

Pagamentos: Cerveja 5, Sopa 6, Carne 8 (em `OrderManager`). A meta de 100 fica em `EconomyManager`. O jogo dura cerca de 10 a 13 minutos.

---

## G. Checklist de testes

1. **Movimento.** No Play, WASD e as setas movem o taverneiro com aceleração suave.
2. **Colisões.** Ele não atravessa paredes, balcão, fogão, barril, mesas nem a porta.
3. **Clientes.** Após a introdução do dia, o primeiro cliente aparece na porta em cerca de 2 s, caminha até uma mesa livre e senta. Dois clientes nunca dividem a mesma mesa.
4. **Pedidos.** O balão sobre o cliente mostra a cor/ícone do produto e a barra de paciência diminui.
5. **Pegar.** Perto do barril, o contorno amarelo e o texto "[E] Pegar Cerveja (Barril)" aparecem; ao apertar E, o ícone surge sobre a cabeça e no HUD.
6. **Trocar e devolver.** Com Cerveja na mão, E no fogão troca por Carne/Sopa. E na mesma estação devolve.
7. **Entregar errado.** O aviso "Pedido errado!" aparece e o item continua na mão.
8. **Entregar certo.** Aparecem "+N moedas", o HUD soma as moedas, a caixa de diálogo mostra o nome e a fala, e o jogo pausa.
9. **Diálogo.** E completa o texto e depois avança; Esc fecha. Ao fechar, o cliente sai pela porta e a mesa é liberada.
10. **Desistência.** Sem atender, a barra fica vermelha e, ao zerar, aparece "cansou de esperar…". O cliente sai sem pagar e conta como perdido.
11. **Dias.** Ao resolver todos os clientes (ou acabar o tempo), aparece o Resumo do Dia com atendidos, perdidos, moedas do dia e total. "Próximo Dia" (clique ou Enter) inicia o dia seguinte, e as moedas se mantêm.
12. **Limite diário.** O número de clientes por dia é exatamente 3/3/5/5/5.
13. **Vitória.** Termine o Dia 5 com 100 moedas ou mais: aparece a tela verde com o texto dos heróis e o total.
14. **Derrota.** Termine o Dia 5 com menos de 100 (deixe clientes desistirem): aparece a tela vermelha com o texto do fechamento.
15. **Botões.** "Jogar/Tentar Novamente" reinicia no Dia 1 com 0 moedas; "Menu Inicial" volta ao menu; "Sair" encerra o Play no Editor ou fecha o jogo no build.
16. **Áudio.** Com clipes atribuídos, a música do menu toca e troca para a da taverna; os efeitos tocam sem cortar a música. Sem clipes, nada quebra.
17. **Console.** Não deve haver erros nem exceções durante a campanha inteira.

## Interface em pixel art

Toda a UI (menu, créditos, HUD, diálogo, resumo do dia e finais) usa um estilo pixel art gerado pelo script `Assets/_Project/Scripts/Editor/PixelUIStyle.cs`, chamado automaticamente por **Guardiao do Farol > Construir Projeto Completo**.

| Arquivo (Assets/_Project/Art/UI) | Uso |
|---|---|
| `UI_Painel.png` | Moldura de madeira 9-slice (barra do HUD, caixas de créditos/resumo/final) |
| `UI_Pergaminho.png` | Papel 9-slice (diálogo, textos de créditos/resumo/final, avisos) |
| `UI_Botao.png`, `UI_Botao_Destaque.png`, `UI_Botao_Pressionado.png` | Botões com troca de sprite (normal, selecionado/hover, pressionado) |
| `UI_Slot.png` | Moldura do item carregado |
| `UI_Moeda.png` | Ícone de moeda |
| `UI_FundoMenu.png` | Fundo da tela inicial (320x180): céu noturno, lua, mar, falésia, farol com feixe de luz e a taverna |

- **Fonte:** [Pixelify Sans](https://fonts.google.com/specimen/Pixelify+Sans) (licença SIL Open Font License 1.1; o texto da licença está em `Assets/_Project/Fonts/PixelifySans_OFL.txt`). O asset TMP é `Assets/_Project/Fonts/PixelifySans SDF.asset`, e os títulos usam o material `PixelifySans SDF - Sombra` (sombra pixelada via *underlay*).
- **Para mudar o visual**, edite as cores ou desenhos em `PixelUIStyle.cs` e rode o menu de construção de novo. Também dá para substituir os PNGs por arte própria, mantendo as bordas 9-slice no Sprite Editor.
- Os botões funcionam com mouse e teclado: o botão selecionado fica com a borda dourada.

## Arte importada (taverna, tela de título e taverneira)

Arquivos em `Assets/_Project/Art/Imported`, aplicados por `Assets/_Project/Scripts/Editor/ImportedArt.cs` (roda no fim de **Construir Projeto Completo**, ou sozinho pelo menu **Guardiao do Farol > Aplicar Arte Importada**). Se um arquivo não existir, aquela parte é simplesmente pulada.

| Arquivo | O que faz |
|---|---|
| `Taverna_Interior.png` | Fundo pintado da taverna (76 px por unidade). O tilemap continua só para colisão (renderer desligado). Balcão, lareira (Carne e Sopa), barril (Cerveja) e porta foram reposicionados sobre a pintura e tiveram o visual antigo escondido. Uma colisão `ParedeFundo` impede andar na parede alta da imagem, e o balde de gelo também bloqueia. As mesas do fundo desceram para y = 1, e o corredor dos clientes passou para y = -0,9. |
| `Tela_Titulo.png` | Fundo da tela inicial. Como a arte já tem o título na placa, os textos de título e subtítulo foram ocultados e o painel de botões foi para a direita, sobre o mar. |
| `Taverneira/Taverneira_<Animação>_<n>.png` | Quadros da personagem jogável (80 px por unidade, pivô nos pés): `Parada` (4), `Andar_Baixo` (7), `Andar_Cima` (8), `Andar_Esquerda` (7), `Andar_Direita` (7) e `Interagir` (4, ainda sem uso). Recortados do sprite sheet: fundo branco removido e quadros alinhados pelos pés num tamanho fixo de 100x140. |

A animação é feita pelo `CharacterAnimator`, sem Animator Controller: se os campos *Idle/Walk Frames* estiverem preenchidos, ele troca os quadros conforme a direção (10 fps andando, 3 fps parada). Parada olhando para cima ou para os lados, mostra o primeiro quadro da caminhada daquela direção. Para trocar a personagem, substitua os PNGs mantendo os nomes e rode **Aplicar Arte Importada**.

## Áudio e animação dos clientes

Os sons ficam em `Assets/_Project/Audio` e foram sintetizados (chiptune) especialmente para o jogo. O script `AudioLibrary.cs` (menu **Guardiao do Farol > Aplicar Áudio**) configura a importação e liga cada clipe no prefab `AudioManager`. Para trocar um som, substitua o WAV mantendo o nome.

| Arquivo | Quando toca |
|---|---|
| `Music/Musica_Menu.wav` | Tela inicial: valsa calma em lá menor, com ondas ao fundo |
| `Music/Musica_Taverna.wav` | Durante o jogo: jiga animada em ré maior |
| `SFX_Passo` | Passos da taverneira |
| `SFX_Pegar` | Pegar ou devolver produto |
| `SFX_Entregar` + `SFX_Moeda` | Entrega correta e pagamento |
| `SFX_Errado` | Entrega do item errado |
| `SFX_Cliente_Entra` | Sininho da porta quando um herói entra |
| `SFX_Cliente_Sai` | Herói perdeu a paciência e foi embora |
| `SFX_Sino` | Abertura e fim do expediente |
| `SFX_Fim_Do_Dia` | Resumo do dia |
| `SFX_Dialogo` | Abertura de diálogo |
| `SFX_Clique` | Botões |
| `SFX_Vitoria` / `SFX_Derrota` | Finais |

Os heróis-clientes têm dois quadros de passo gerados (`Heroi_<Nome>_Passo1/2.png`). A campanha guarda esses quadros em `CustomerProfile.walkFrames`, e o `CustomerController` repassa ao `CharacterAnimator` com `SetFrames`. Andando, o herói alterna os passos (espelhado quando vai para a esquerda). Sentado, fica parado.

### Limitações conhecidas
- A taverneira tem animação; os heróis-clientes, mesas e cadeiras ainda usam a pixel art gerada de 16 px (estática), num estilo mais simples que o da pintura da taverna.
- Os clientes não desviam do jogador (atravessam visualmente), por decisão de simplicidade (waypoints sem pathfinding).

---

## Créditos

*O Guardião do Farol* é um jogo desenvolvido para a matéria de **Desenvolvimento de Jogos Digitais** do professor **Luis Filipe Grael Tinós**.

- **Desenvolvedora:** Marcus Vinícius da Silva Capeteruchi
- **Curso:** Ciência da Computação
- **Instituição:** UNISAGRADO
- **Professor:** Luis Filipe Grael Tinós
- **Disciplina:** Desenvolvimento de Jogos Digitais

Fonte: Pixelify Sans, © 2021 The Pixelify Sans Project Authors, sob a SIL Open Font License 1.1.

Para alterar esse texto no jogo, edite o objeto `Canvas/Creditos/Caixa/Pergaminho/Texto` na cena `MainMenu` ou o painel de créditos em `GuardiaoProjectBuilder.BuildMainMenuScene`.
