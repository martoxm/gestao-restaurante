const formatadorData = new Intl.DateTimeFormat("pt-BR", { dateStyle: "short" });

export function formatarData(dataIso: string): string {
  const temFuso = /Z$|[+-]\d{2}:\d{2}$/.test(dataIso);
  return formatadorData.format(new Date(temFuso ? dataIso : `${dataIso}Z`));
}
