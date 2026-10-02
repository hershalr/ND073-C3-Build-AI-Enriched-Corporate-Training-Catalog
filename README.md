# AI-Enriched Corporate Training Catalog

Udacity ND073 Course 3 project — Azure AI Search knowledge mining (courses + research library), custom enrichments, and a Vue static site.

## Graded artifacts

| Path | Contents |
|------|----------|
| `submission/step_1/` | Requirements + architecture diagram |
| `submission/step_2/` | Import skillsets/indexes/indexers + queries + data-sources screenshot |
| `submission/step_3/` | Moodle Custom Entity Lookup + Springer Web API skill defs + advanced queries |
| `submission/step_4/` | Monitoring screenshots + configured `index.html` |
| `starter/Function/` | SpringerLookup Azure Function (.NET 8 isolated) source |
| `starter/Website/index.html` | Vue search UI (service URL + courses index wired; query key placeholder) |
| `starter/Data/` | Sample courses CSV, instructor profiles, library PDFs |

## Static website (lab)

Primary endpoint used during the lab session:

`https://catalogstore1002.z21.web.core.windows.net/`

Search service: `catalog-search-1002`  
Courses index: `search-1790894939519`  
Library index: `search-1790884495622`

The committed `index.html` uses `api-key: '<insert-query-key>'` so GitHub push protection is not tripped. The live `$web` host was configured with the lab query key.

## Submit

1. Upload `CorporateTrainingCatalog-FINAL.zip` (same tree as this repo: `starter/` + `submission/` + this README).
2. Paste this repository URL on the submission page.
