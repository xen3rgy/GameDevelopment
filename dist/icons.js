const paths={
 bottle:'M9 3h6M10 3v5l-3 5v15h10V13l-3-5V3M7 16h10M7 24h10',
 water:'M9 3h6M10 3v5l-3 5v15h10V13l-3-5V3M7 17c3-3 7 3 10 0',
 coffee:'M5 10h16v11a6 6 0 0 1-6 6h-4a6 6 0 0 1-6-6ZM21 12h3a4 4 0 0 1 0 8h-3M10 3v3M16 3v3',
 sandwich:'m4 22 12-15 12 15ZM4 26h24M10 16h12',
 meal:'M4 17h24a12 12 0 0 1-24 0ZM8 28h16M10 4v6M16 3v7M22 4v6',
 medicine:'M8 4h16v24H8ZM12 16h8M16 12v8',
 parcel:'m4 10 12-6 12 6-12 6ZM4 10v14l12 6 12-6V10M16 16v14M10 7l12 6',
 pasta:'M8 4h16l2 24H6ZM9 10h14M11 15v8M16 14v9M21 15v8',
 vegetables:'M16 10c-10-7-16 13 0 18 16-5 10-25 0-18ZM16 10V5l5-2',
 bread:'M7 26V13C-1 4 10 0 16 6c6-6 17-2 9 7v13ZM10 13v6M16 11v8M22 13v6',
 cheese:'m4 18 24-9v18H4ZM4 18h24M21 14v1M12 22v1M23 23v1'
};
export function itemIcon(id){return `<svg class="item-symbol" viewBox="0 0 32 32" fill="none" stroke="currentColor" stroke-width="1.55" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="${paths[id]||paths.parcel}"/></svg>`}
