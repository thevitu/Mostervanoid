# Mostervanoid

Plataforma 2D no estilo clássico de ação/tiro, com uma lata de energético como protagonista.
Personagens em 3D e cenário com profundidade 3D em camadas (paralaxe de verdade com câmera em perspectiva).

**Unity 6000.6.7f1** · URP 17.6 · Input Manager clássico.

## Baixar e jogar (alpha 0.1, Windows)
1. Baixe `Mostervanoid-alpha-0.1-windows.zip` em **[Releases](https://github.com/thevitu/Mostervanoid/releases)**.
2. Extraia o zip e rode `Mostervanoid.exe` (o Windows pode mostrar o aviso do SmartScreen: *Mais informações* > *Executar assim mesmo*).
3. Encontrou um bug? Abra uma *Issue* aqui no GitHub contando a fase e o que aconteceu.

## Como rodar (projeto Unity)
1. Abra a pasta `Mostervanoid` no Unity Hub (versão 6000.6.7f1).
2. Abra `Assets/Scenes/Main.unity` e aperte **Play**.
   (Qualquer cena funciona: o jogo se monta sozinho por código.)
3. Menu **Mostervanoid > Build Windows** gera `Builds/Windows/Mostervanoid.exe`.

## Controles
| Ação | Tecla |
|---|---|
| Mover | WASD / setas |
| Pular (segure = mais alto) | Espaço / 0 |
| Atirar | K / 2 |
| Ação especial (recarga 3s) | L / 3 |
| Menu de formas | Enter |
| Teleporte | M |
| Cheats | C |
| Sair | Esc (dentro de um menu, Esc fecha o menu) |
| Descer de plataforma | Baixo + Pulo |

## Fases (um chefe por fase; cada chefe libera uma forma)
| Fase | Exige | Chefe | Libera |
|---|---|---|---|
| Selva Gaseificada | — | Galão d'Água | 1 Slide |
| Torre da Refinaria | Slide | Barril de Vinho (deslize por baixo, atire na rolha) | 2 Super Pulo |
| Vinhedo ao Entardecer | Slide + Super Pulo | Garrafa de Refri (super pule até as plataformas altas) | 3 Dash |
| Geleira Engarrafada | Slide + Super Pulo + Dash | Coqueteleira (atravesse os muros de gelo com o Dash) | 4 Pulo Duplo |
| Céu Borbulhante | todas as anteriores, foco no Pulo Duplo | Garrafa de Champanhe (rajadas de muros de espuma em sequência; atire nela no meio do pulo duplo) | 5 Impacto no Chão |
| Caverna Efervescente (de cima para baixo) | formas 1–5, foco no Impacto no Chão (pisos rachados) | Bule de Chá (reflete tiros; só o tremor do impacto no chão o racha) | 6 Gancho |
| Chaminé do Café (de baixo para cima) | formas 1–6, foco no Gancho (vãos com espinhos) | Caixinha de Suco (inunda o chão: pendure-se nos ganchos) | 7 Impacto na Parede |
| Fábrica de Vidro | todas, foco no Impacto na Parede (portas reforçadas) | Jarra de Limonada (prende num copo de vidro: impacto na parede para sair) | acesso ao final |
| Labirinto do Minotauro (a mais longa) | todas as 7 | Minotauro Vermelho (150 de vida, 3 fases de fúria; vencível com qualquer forma) | animação final |

Cada chefe só exige o **último poder ganho** (não precisa trocar de forma na luta) e o ritmo dos ataques respeita a recarga de 3s.
Morreu? Volta ao início da fase ou ao último teleporte (pós-chefe) tocado.

## Formas
| # | Sabor | Habilidade (L/3) |
|---|---|---|
| 0 | ORIGINAL | Tiro especial, 5x o dano |
| 1 | CITRUS | Slide (passa por frestas de 1 bloco) |
| 2 | BERRY | Super pulo (segure e solte, ~3x a altura) |
| 3 | MANGO | Dash invulnerável (funciona no ar, atravessa projéteis, inimigos e jatos de vapor) |
| 4 | ICE | Pulo duplo (passivo) + tiro especial |
| 5 | COLA | Impacto no chão (no ar; quebra piso rachado `G`) |
| 6 | KIWI | Gancho (segure no ar perto de `H`, até 3s, sem recarga) |
| 7 | CHILI | Impacto na parede (quebra portas reforçadas `X`) |


## Onde customizar
- **Skins da lata:** `Assets/StreamingAssets/Skins/form0.png … form7.png` (512x256, enrolada na lata; o centro da imagem fica de frente). No jogo: `C` > *Recarregar skins da pasta*.
- **Fases:** `Assets/Resources/Levels/stageN.txt` são mapas em texto (legenda no `README.txt` da mesma pasta).
- **Registro das fases** (tema, chefe, forma liberada, próxima fase): `Assets/Scripts/Core/Stages.cs`.
- **Ajuste fino da física** (pulo, slide, dash…): constantes no topo de `Assets/Scripts/Actors/PlayerController.cs`.

## Teste automático
`Mostervanoid.exe -mvtest -mvout <pasta>`: simula entradas, mede pulo e super pulo, testa o slide na fresta,
os 4 chefes, o inimigo voador e os menus, salva screenshots e fecha. Não mexe no progresso salvo.
Seções avulsas com `-mvonly <nome>`: `coqueteleira`, `ceu`, `vapor`, `bule`, `suco`, `jarra`, `minotauro` (inclui o final), `fases`, `finais` (todas as novas).

## Cheats (tecla C)
Invencível, **Vida infinita** (leva o golpe mas não perde vida nem morre), sem recarga, tiro mata com 1 acerto, voo livre, liberar formas/chefes, ir para a sala do chefe, derrotar o chefe atual, ir para qualquer fase (←/→), recarregar skins e resetar progresso.
