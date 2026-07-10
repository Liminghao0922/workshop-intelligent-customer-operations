document$.subscribe(async () => {
  const mermaidDefinition = /^\s*(graph|flowchart|sequenceDiagram)\b/;
  const blocks = [...document.querySelectorAll("pre > code")].filter((block) =>
    mermaidDefinition.test(block.textContent),
  );

  for (const block of blocks) {
    const container = document.createElement("div");
    container.className = "mermaid";
    container.textContent = block.textContent;
    const wrapper = block.closest(".highlight") ?? block.parentElement;
    wrapper.replaceWith(container);
  }

  await mermaid.run({
    nodes: document.querySelectorAll(".mermaid"),
  });
});
