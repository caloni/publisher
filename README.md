# Formato de Publicação

## 1. Filosofia

O formato de publicação foi concebido para armazenar textos de maneira simples, duradoura e independente de ferramentas específicas.

O conteúdo deve permanecer legível e compreensível mesmo sem a existência do publicador ou de qualquer ferramenta de processamento.

A publicação é composta por três elementos independentes:

- **Conteúdo:** arquivos de texto puro contendo as entradas.
- **Metadados:** arquivos YAML que associam elementos do conteúdo a recursos externos.
- **Publicador:** ferramenta responsável por interpretar o conteúdo e os metadados e produzir as representações publicadas.

O publicador não é proprietário do conteúdo. Ele apenas interpreta suas convenções e produz uma representação derivada.

## 2. Arquivos de conteúdo

Uma publicação pode ser composta por um ou mais arquivos de conteúdo. Cada arquivo pode conter inúmeras entradas independentes.

Cada entrada possui a seguinte estrutura:

```text
# Título da entrada
AAAA-MM-DD tag1 tag2 tag3

Texto da entrada começa aqui.

Pode conter vários parágrafos, listas e outras
construções textuais.
```

### 2.1. Título

O título é iniciado pelo marcador `#`, seguido de um espaço e do texto do título.

O título também funciona como identificador da entrada, sendo utilizado pelo publicador para estabelecer referências e relações com os metadados.

### 2.2. Data e tags

A linha imediatamente posterior ao título contém a data da entrada e, opcionalmente, suas tags.

A data utiliza o formato ISO 8601 simplificado:

```text
AAAA-MM-DD
```

As tags são identificadores textuais separados por espaços e pertencem à mesma linha da data.

Exemplo:

```text
2005-05-15 computer blog
```

A data e as tags são utilizadas pelo publicador para organizar e relacionar as entradas, permitindo, entre outras possibilidades:

- Listar entradas de uma mesma tag.
- Agrupar entradas por ano ou mês.
- Ordenar cronologicamente as publicações.

### 2.3. Corpo do texto

Uma linha vazia após a linha de data e tags indica o início do corpo da entrada.

O corpo é composto por texto puro, sem dependência de HTML, links ou qualquer mecanismo de renderização.

São permitidas convenções textuais simples para organizar o conteúdo, como:

- Parágrafos separados por linhas vazias.
- Listas.
- Subtítulos e cabeçalhos.
- Expressões entre aspas.

Essas convenções devem preservar a legibilidade do conteúdo em um editor de texto convencional.

O conteúdo não precisa conhecer os recursos que serão associados a ele durante a publicação.

## 3. Convenção de aspas

Expressões delimitadas por aspas duplas podem funcionar como pontos de associação com recursos externos.

As aspas, por si só, não definem uma operação ou significado especial. Uma expressão entre aspas somente recebe um tratamento diferenciado quando existe uma associação correspondente nos metadados.

Exemplo:

```text
Estudei "uma tal de C" durante muitos anos.
Depois comecei a desenvolver jogos como "genius".
```

Nesse exemplo, ambas as expressões são apenas texto. O publicador não deve presumir que sejam links ou qualquer outro tipo de recurso.

Quando existir uma associação nos metadados, o publicador poderá enriquecer a representação da expressão sem modificar o conteúdo original.

As associações podem representar, por exemplo:

- Links externos.
- Imagens da biblioteca de imagens.
- Outros recursos suportados pelo publicador.

A mesma convenção pode ser utilizada para expressões que ocupem um parágrafo inteiro, como uma referência a uma imagem:

```text
"imagem.png"
```

O comportamento depende exclusivamente dos metadados associados.

## 4. Metadados

Os metadados são armazenados em arquivos YAML independentes dos arquivos de conteúdo.

Sua finalidade é estabelecer associações entre expressões existentes no conteúdo e recursos utilizados durante a publicação.

Uma entrada de metadados contém, no mínimo:

- `description`: expressão textual que identifica o trecho correspondente no conteúdo.
- `link`: destino ou referência do recurso associado.

Exemplo:

```yaml
- description: "imagem.png"
  link: img/imagem.png

- description: "uma tal de C"
  link: https://www.linguagem-c.org.br

- description: "Autor"
  link: https://www.caloni.com.br
```

### 4.1. Associação com o título

O título de uma entrada também pode possuir uma associação nos metadados.

Nesse caso, a associação utiliza o próprio título como `description`.

Exemplo:

```yaml
- description: "Autor"
  link: https://www.caloni.com.br
```

Durante a publicação, o título poderá ser apresentado como um link para o destino correspondente.

### 4.2. Associação com expressões do corpo

Uma expressão entre aspas pode possuir uma associação nos metadados, independentemente de sua posição no texto.

Exemplo:

```text
... passou a estudar "uma tal de C", linguagem que o faria
ficar ainda mais doidão por programação.
```

Com a seguinte associação:

```yaml
- description: "uma tal de C"
  link: https://www.linguagem-c.org.br
```

O publicador poderá transformar a expressão em um link para o destino indicado.

### 4.3. Associação com imagens

Uma expressão que ocupe um parágrafo inteiro também pode ser associada a uma imagem.

Exemplo:

```text
"imagem.png"
```

Metadados:

```yaml
- description: "imagem.png"
  link: img/imagem.png
```

O publicador interpreta a associação conforme as convenções de recursos da biblioteca de imagens.

### 4.4. Ausência de associação

Uma expressão sem associação nos metadados permanece como texto comum.

Não existe obrigação de cadastrar todas as expressões entre aspas.

O conteúdo deve continuar válido e compreensível mesmo quando os metadados não estiverem disponíveis.

## 5. Responsabilidades do publicador

O publicador interpreta os arquivos de conteúdo e seus respectivos metadados para produzir as representações publicadas.

Entre suas responsabilidades estão:

1. Identificar as entradas nos arquivos de conteúdo.
2. Extrair títulos, datas e tags.
3. Identificar o corpo de cada entrada.
4. Consultar os metadados para localizar associações.
5. Resolver links e recursos da biblioteca de imagens.
6. Produzir as páginas e demais representações da publicação.
7. Construir índices cronológicos e agrupamentos por tags.

A apresentação final pode conter elementos que não existem explicitamente nos arquivos de conteúdo, como links, imagens, navegação e índices.

Esses elementos são derivados e não precisam ser incorporados ao texto original.

## 6. Exemplo completo

### Conteúdo

```text
# Autor
2005-05-15 computer blog

Compulsive programmer. Wanderley Caloni Jr, brasileiro
de nascença e programador por paixão, começou a se
divertir aos 17 anos com seu Microsoft Quick BASIC
recém baixado da internet.

Depois que fez seu primeiro programa - uma calculadora -
e saiu pulando pela casa feliz da vida, nunca mais parou
de programar.

Após dominar as técnicas básicas do if, while e for
(ohhhhh...) passou a estudar "uma tal de C", linguagem
que o faria ficar mais doidão ainda por programação.

Seu primeiro programa (in)útil foi o jogo "genius",
o único no mundo (que eu conheço) que guardava recordes
no próprio executável!

Depois que gabaritou no C passou a desenvolver classes
templates com funções polimórficas e herança múltipla
em C++.

"imagem.png"
```

### Metadados

```yaml
- description: "imagem.png"
  link: img/imagem.png

- description: "uma tal de C"
  link: https://www.linguagem-c.org.br

- description: "Autor"
  link: https://www.caloni.com.br
```

### Resultado conceitual

- O título `Autor` é associado à página inicial indicada nos metadados.
- A expressão `uma tal de C` é associada a um link externo.
- A expressão `imagem.png` é associada a uma imagem da biblioteca.
- A expressão `genius` permanece como texto comum, pois não possui associação.
- A data e as tags permitem que a entrada seja encontrada em índices cronológicos e temáticos.

## 7. Princípios de preservação

O formato deve preservar as seguintes propriedades:

- **Legibilidade:** os arquivos de conteúdo devem ser compreensíveis sem ferramentas especiais.
- **Portabilidade:** o conteúdo deve poder ser editado em qualquer editor de texto.
- **Independência:** o conteúdo não deve depender dos metadados para continuar legível.
- **Separação:** recursos de publicação não devem exigir modificações no texto original.
- **Reprodutibilidade:** a publicação deve poder ser reconstruída a partir dos arquivos de conteúdo, metadados e recursos necessários.
- **Simplicidade:** as convenções devem permanecer pequenas, explícitas e fáceis de implementar em diferentes linguagens e plataformas.

A longevidade do conteúdo é mais importante que a longevidade de qualquer implementação específica do publicador.


## 8. Convenções adicionais e limitações

Esta seção documenta características e limitações da sintaxe textual utilizada pelo publicador.

### 8.1. Listas

As listas são representadas por marcadores textuais simples, sem depender de recursos de renderização.

As listas possuem estrutura exclusivamente plana:

- Não existem listas aninhadas.
- Um item não pode conter outra lista.
- Não é permitido inserir citações dentro de listas.

Essas restrições são intencionais e preservam a simplicidade da interpretação do conteúdo.

Uma lista deve ser tratada como uma sequência de itens independentes.

Exemplo:

```text
Algumas características:

- Primeiro item.
- Segundo item.
- Terceiro item.
```

### 8.2. Citações

Citações podem ser utilizadas no corpo do texto conforme as convenções reconhecidas pelo publicador.

Não é permitido inserir citações dentro de listas.

## 9. Próximas mudanças no padrão

Esta seção reúne sugestões de evolução do formato. Os itens abaixo não fazem necessariamente parte da especificação atual e dependem de avaliação e implementação futuras.

### 9.1. Identificador permanente de entradas

**Status:** sugestão futura, ainda não implementada.

Considerar o uso de uma tag especial iniciada pelo caractere `@` para identificar permanentemente uma entrada.

Exemplo:

```text
# Autor
2005-05-15 computer blog @autor

Compulsive programmer...
```

A proposta é aproveitar a estrutura existente de tags, sem introduzir um novo campo ou uma linha adicional.

Regras propostas:

- Uma única tag iniciada por `@` identifica a entrada.
- O identificador deve ser único em todo o acervo.
- O identificador não depende do título nem da localização física do arquivo.
- O identificador permanece inalterado mesmo quando o título ou o conteúdo são modificados.
- A ausência de um identificador não invalida a entrada.
- Mais de uma tag iniciada por `@` na mesma entrada deve ser considerada inválida.

O identificador poderá ser utilizado como referência interna e como slug na publicação.

Sugere-se restringir sua composição a letras ASCII minúsculas, números e hífens, evitando problemas de codificação e geração de URLs.

### 9.2. Desambiguação de títulos por data

**Status:** sugestão futura, ainda não implementada.

Considerar a utilização da data como parte da identidade de uma entrada quando não houver identificador permanente.

A proposta é permitir que títulos iguais existam no acervo, desde que publicados em datas diferentes.

Por exemplo:

```text
# Reflexões
2020-05-15 pensamentos

Primeiro texto.

# Reflexões
2021-08-20 pensamentos

Segundo texto.
```

Nesse cenário, a combinação de título e data permite distinguir as entradas.

A data poderá ser utilizada também na construção de referências internas e URLs, evitando colisões entre títulos.

A solução deverá considerar separadamente a possibilidade de títulos iguais publicados na mesma data.

### 9.3. Escopo dos metadados por entrada

**Status:** sugestão futura, ainda não implementada.

Considerar a associação explícita dos recursos do YAML a uma entrada específica, utilizando seu identificador permanente.

Exemplo:

```yaml
- entry: "@autor"
  description: "uma tal de C"
  link: https://www.linguagem-c.org.br
```

A proposta elimina a ambiguidade de expressões iguais em diferentes entradas e permite que cada texto mantenha suas próprias associações.

O título e as expressões do corpo poderão ser utilizados como descrições dos recursos associados.

A ausência de uma associação continuará significando que a expressão deve ser apresentada como texto comum.

### 9.4. Desambiguação de expressões entre aspas

**Status:** sugestão futura, ainda não formalizada.

Atualmente, expressões entre aspas podem funcionar como pontos de associação com recursos definidos nos metadados.

Uma questão ainda em aberto é o comportamento quando a mesma expressão aparece mais de uma vez no corpo de uma entrada.

Uma possibilidade é exigir que a expressão associada seja única dentro da entrada.

Caso existam múltiplas ocorrências da mesma expressão, o publicador poderá sinalizar a ambiguidade em vez de escolher silenciosamente qual ocorrência deve receber o recurso.

Essa abordagem transfere ao autor a responsabilidade de fornecer referências textuais suficientemente específicas.

A repetição de uma expressão sem associação nos metadados continuará sendo permitida e não deverá produzir qualquer interpretação especial.

### 9.5. Notas de rodapé

**Status:** sugestão futura, ainda não implementada.

Considerar um mecanismo simples de notas de rodapé baseado em números entre parênteses.

Uma expressão numérica entre parênteses no corpo do texto poderá representar uma referência a uma nota de rodapé, desde que exista uma definição correspondente na própria entrada.

Exemplo de referência:

```text
Este é um texto com uma nota (1) e outra referência (2).
```

As definições poderão ser apresentadas no final do corpo da entrada:

```text
(1) Esta é a primeira nota de rodapé.

(2) Esta é a segunda nota de rodapé.
```

O mecanismo deverá obedecer às seguintes premissas:

- Os números entre parênteses, isoladamente, não possuem significado especial.
- Uma referência somente será reconhecida quando existir uma definição correspondente.
- Números sem definição correspondente permanecem como texto comum.
- As definições devem ser identificáveis independentemente de sua posição no texto.
- Recomenda-se concentrar as definições ao final da entrada, embora a obrigatoriedade dessa posição ainda precise ser avaliada.
- A apresentação poderá transformar as referências em ligações para as respectivas notas, permitindo também o retorno ao ponto de origem.

Exemplo completo:

```text
# Testando rodapé (sem)
2026-11-15 testes

Este é um teste com um texto sem rodapé e com (2)
elementos que poderiam virar.

# Testando rodapé (com)
2026-11-15 testes

Este é um teste com um texto com rodapé (1) e com
(2) elementos que poderiam virar.

(1) Por que o exemplo de cima são só números com parênteses.

(2) Este é um segundo exemplo.
```

No primeiro exemplo, `(2)` permanece como texto comum porque não existe uma definição correspondente.

No segundo, `(1)` e `(2)` são referências às notas definidas no próprio corpo da entrada.

A ausência de notas de rodapé não altera a interpretação de números entre parênteses.

### 9.6. Princípio de evolução

As mudanças futuras devem preservar as propriedades fundamentais do formato:

- Compatibilidade com arquivos de conteúdo existentes.
- Legibilidade sem ferramentas especializadas.
- Ausência de dependência de formatos binários ou proprietários.
- Separação entre conteúdo e metadados.
- Regras pequenas e determinísticas.
- Possibilidade de implementação independente em diferentes linguagens.

Novas convenções devem ser introduzidas somente quando oferecerem benefícios concretos sem comprometer a simplicidade do formato.
