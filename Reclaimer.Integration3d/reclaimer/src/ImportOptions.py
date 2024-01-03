from dataclasses import dataclass
from pathlib import Path

from .Material import *
from .Model import *

__all__ = [
    'ImportOptions'
]


@dataclass
class ImportOptions:
    IMPORT_BONES: bool = True
    IMPORT_MARKERS: bool = True
    IMPORT_MESHES: bool = True
    IMPORT_NORMALS: bool = True
    IMPORT_SKIN: bool = True
    IMPORT_UVW: bool = True
    IMPORT_MATERIALS: bool = True

    SPLIT_MESHES: bool = False

    BONE_SCALE: float = 1.0
    MARKER_SCALE: float = 1.0

    BONE_PREFIX: str = ''
    MARKER_PREFIX: str = '#'

    BITMAP_ROOT: str = ''
    BITMAP_EXT: str = 'tif'

    def model_name(self, model: Model):
        return f'{model.name}'

    def bone_name(self, bone: Bone):
        return f'{self.BONE_PREFIX}{bone.name}'

    def marker_name(self, marker: Marker, index: int):
        return f'{self.MARKER_PREFIX}{marker.name}'

    def region_name(self, region: ModelRegion):
        return f'{region.name}'

    def permutation_name(self, region: ModelRegion, permutation: ModelPermutation, index: int):
        return f'{region.name}:{permutation.name}'

    def material_name(self, material: Material):
        return f'{material.name}'

    def texture_path(self, texture: Texture):
        path = Path(self.BITMAP_ROOT).joinpath(texture.name).with_suffix('.' + self.BITMAP_EXT.lstrip('.'))
        return str(path)