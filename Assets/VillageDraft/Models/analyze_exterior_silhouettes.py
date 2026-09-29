"""Measure front/top silhouette difference for all 15 Blender venue exteriors.

Run audit_exterior_silhouettes.py in Blender first, then this script with Python
and Pillow. The common walkable platform is excluded by the render script.
The pair score is the mean Jaccard intersection/union of front and top masks.
"""
from itertools import combinations
from pathlib import Path
import json
import os

from PIL import Image


ROOT = Path(__file__).resolve().parent
MASKS = Path(os.environ.get('VILLAGE_AUDIT_ROOT', '/tmp/village_model_audit')) / 'exterior_silhouettes'
VENUES = sorted(path.stem[:-6] for path in MASKS.glob('*_front.png'))


def opaque_pixels(path):
    image = Image.open(path).convert('RGBA')
    return {i for i, pixel in enumerate(image.get_flattened_data())
            if pixel[0] > 128 and pixel[3] > 128}


def overlap(a, b):
    return len(a & b) / len(a | b)


if len(VENUES) != 15:
    raise SystemExit(f'Expected 15 front masks, found {len(VENUES)}')

masks = {(venue, view): opaque_pixels(MASKS / f'{venue}_{view}.png')
         for venue in VENUES for view in ('front', 'top')}
scores = []
for first, second in combinations(VENUES, 2):
    front = overlap(masks[first, 'front'], masks[second, 'front'])
    top = overlap(masks[first, 'top'], masks[second, 'top'])
    scores.append({'first': first, 'second': second,
                   'front_iou': round(front, 4), 'top_iou': round(top, 4),
                   'mean_iou': round((front + top) / 2, 4)})
scores.sort(key=lambda item: item['mean_iou'], reverse=True)
report = {
    'method': 'Mean binary front/top Jaccard IoU, excluding common floor platform',
    'target_max_mean_iou': 0.60,
    'venue_count': len(VENUES),
    'pair_count': len(scores),
    'pairs_above_target': sum(item['mean_iou'] > .60 for item in scores),
    'mean_pair_iou': round(sum(item['mean_iou'] for item in scores) / len(scores), 4),
    'worst_pair': scores[0],
    'pairs': scores,
}
path = ROOT / 'silhouette_audit.json'
path.write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
print(f"{report['venue_count']} venues, {report['pair_count']} pairs, "
      f"{report['pairs_above_target']} pairs above 0.60, "
      f"mean IoU {report['mean_pair_iou']}; worst: {report['worst_pair']}")
