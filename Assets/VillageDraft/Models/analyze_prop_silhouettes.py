"""Report pairwise prop silhouette overlap, emphasizing repeated modeling roles."""
from itertools import combinations
from pathlib import Path
import json
import os

from PIL import Image

ROOT = Path(__file__).resolve().parent
manifest = json.loads((ROOT / 'asset_manifest.json').read_text(encoding='utf-8'))
assets = [asset for venue in manifest['venues'].values() for asset in venue['assets'][1:]]
mask_dir = Path(os.environ.get('VILLAGE_AUDIT_ROOT', '/tmp/village_model_audit')) / 'prop_silhouettes'
views = ('front', 'top', 'three_quarter')


def pixels(path):
    image = Image.open(path).convert('RGBA')
    return {index for index, pixel in enumerate(image.get_flattened_data())
            if pixel[0] > 128 and pixel[3] > 128}


masked = {(asset['name'], view): pixels(mask_dir / f"{asset['name']}_{view}.png")
          for asset in assets for view in views}


def iou(a, b):
    return len(a & b) / len(a | b)


scores = []
for first, second in combinations(assets, 2):
    front = iou(masked[first['name'], 'front'], masked[second['name'], 'front'])
    top = iou(masked[first['name'], 'top'], masked[second['name'], 'top'])
    three_quarter = iou(masked[first['name'], 'three_quarter'],
                        masked[second['name'], 'three_quarter'])
    scores.append({'first': first['name'], 'second': second['name'],
                   'same_role': first['role'] == second['role'],
                   'front_iou': round(front, 4), 'top_iou': round(top, 4),
                   'three_quarter_iou': round(three_quarter, 4),
                   'front_top_mean_iou': round((front + top) / 2, 4),
                   'mean_iou': round((front + top + three_quarter) / 3, 4)})
scores.sort(key=lambda item: item['mean_iou'], reverse=True)
same = [item for item in scores if item['same_role']]
report = {
    'method': 'Mean binary front/top/three-quarter Jaccard IoU at common 4.5 m orthographic framing',
    'prop_count': len(assets),
    'pair_count': len(scores),
    'same_role_pair_count': len(same),
    'same_role_pairs_above_0_60': sum(item['mean_iou'] > .60 for item in same),
    'all_pairs_above_0_60': sum(item['mean_iou'] > .60 for item in scores),
    'front_top_pairs_above_0_60': sum(item['front_top_mean_iou'] > .60 for item in scores),
    'worst_same_role': same[:30],
    'worst_any_role': scores[:30],
}
(ROOT / 'prop_silhouette_audit.json').write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
print(f"{len(assets)} props; {len(same)} same-role pairs; "
      f"{report['same_role_pairs_above_0_60']} same-role pairs above 0.60; "
      f"{report['all_pairs_above_0_60']} of {len(scores)} all pairs above 0.60")
for item in same[:15]:
    print(item)
