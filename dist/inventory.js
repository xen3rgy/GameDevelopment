import {ITEMS} from './data.js?v=0.7.2';
export const inventoryWeight=slots=>slots.reduce((sum,v)=>sum+ITEMS[v.id].weight*v.count,0);
export const itemCount=(slots,id)=>slots.filter(v=>v.id===id).reduce((sum,v)=>sum+v.count,0);
export function insertItem(slots,id,count=1,capacity=16,maxWeight=20){
 if(!Object.hasOwn(ITEMS,id)||!Number.isInteger(count)||count<1)return false;
 const item=ITEMS[id],space=slots.filter(v=>v.id===id).reduce((n,v)=>n+item.stack-v.count,0)+(capacity-slots.length)*item.stack;
 if(space<count||inventoryWeight(slots)+item.weight*count>maxWeight+.00001)return false;
 for(const slot of slots)if(slot.id===id){const n=Math.min(count,item.stack-slot.count);slot.count+=n;count-=n}
 while(count){const n=Math.min(count,item.stack);slots.push({id,count:n});count-=n}return true;
}
export function removeItem(slots,id,count=1){
 if(!Number.isInteger(count)||count<1||itemCount(slots,id)<count)return false;
 for(let i=slots.length-1;i>=0&&count;i--){const slot=slots[i];if(slot.id!==id)continue;const n=Math.min(count,slot.count);slot.count-=n;count-=n;if(!slot.count)slots.splice(i,1)}return true;
}
export function transferItem(source,target,index,count,capacity,maxWeight){
 const slot=source[index];if(!slot||slot.id==='parcel'||!Number.isInteger(count)||count<1||count>slot.count)return false;
 if(!insertItem(target,slot.id,count,capacity,maxWeight))return false;
 slot.count-=count;if(!slot.count)source.splice(index,1);return true;
}
