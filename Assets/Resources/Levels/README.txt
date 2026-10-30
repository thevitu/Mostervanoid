FASES DO MOSTERVANOID
=====================

Cada fase é um arquivo de texto (stage1.txt, stage2.txt ...). Cada caractere = 1 bloco.
A linha de cima do mapa é o topo da fase. Linhas podem ter tamanhos diferentes (o resto vira vazio).

Linhas especiais:
  // comentário
  @hint N texto      -> texto da placa "N" (N = dígito 0-9 colocado no mapa)

LEGENDA
  .   vazio
  #   chão / parede sólida
  =   plataforma atravessável (sobe por baixo; BAIXO + PULO desce)
  ^   espinhos (morte instantânea)
  X   porta reforçada  (só a Forma 7 - Impacto na Parede destrói)
  G   piso rachado     (só a Forma 5 - Impacto no Chão destrói)
  H   ponto de gancho  (Forma 6 - segure L/3 no ar perto dele)
  < > jato de vapor empurrando para a esquerda/direita (só o DASH atravessa; use 2 de largura)
  P   início do jogador
  K   checkpoint de meio de fase (suportado, mas não usado: o jogo só usa os teleportes após os chefes)
  0-9 placa de dica (texto em @hint)
  w   inimigo andador         f  inimigo voador
  a   inimigo BLINDADO (só tiro especial / investidas)
  t   torreta
  O   chefe (posição inicial)
  c   centro da câmera durante a luta com o chefe
  [   gatilho que inicia a luta (coloque DENTRO da sala, depois do portão d)
  d   portão de entrada da sala do chefe (fecha quando a luta começa)
  D   portão de saída (abre quando o chefe morre)
  T   teleporte (ativa ao derrotar o chefe; fica sempre APÓS a sala do chefe)
  E   portal para a próxima fase (ativa ao derrotar o chefe)
  b   ponto de "ir para o chefe" do menu de cheats

Física de referência: pulo normal ~3.8 blocos, super pulo ~11 blocos,
jogador em pé tem 1.6 de altura (precisa de 2 blocos livres), deslizando 0.7 (passa em fresta de 1 bloco).

NOVA FASE: crie stageN.txt aqui e registre em Assets/Scripts/Core/Stages.cs
(tema, chefe, forma liberada e 'next'). Aponte o 'next' da fase anterior para ela.
